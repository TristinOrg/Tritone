using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Assets;
using Tritone.Kernel;
using Tritone.UI;
using Tritone.Unity.Assets;
using Tritone.Unity.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies dynamic window registration, loading, ownership, and hot-update replacement.
    /// </summary>
    public sealed class UITests
    {
        /// <summary>
        /// Verifies that multiple owners share one loaded window until the final owner exits.
        /// </summary>
        [Test]
        public void WindowOwners_ShareAndReleaseOneLoadedWindow()
        {
            using UITestEnvironment environment = new();
            var firstScope  = environment.UIService.CreateScope();
            var secondScope = environment.UIService.CreateScope();
            firstScope.AddWindow(typeof(UITestWindow),
                                 "UI/TestWindow",
                                 EUILayer.Normal,
                                 EUIWindowLifetime.Module);
            secondScope.AddWindow(typeof(UITestWindow),
                                  "UI/TestWindow",
                                  EUILayer.Normal,
                                  EUIWindowLifetime.Module);

            var first  = environment.UIService.OpenWindow(typeof(UITestWindow));
            var second = environment.UIService.OpenWindow(typeof(UITestWindow));

            Assert.AreSame(first, second);
            Assert.AreEqual(1, environment.Provider.LoadCount);

            firstScope.Dispose();
            Assert.IsNotNull(environment.UIService.GetWindow(typeof(UITestWindow)));
            Assert.AreEqual(0, environment.Provider.ReleaseCount);

            secondScope.Dispose();
            Assert.IsNull(environment.UIService.GetWindow(typeof(UITestWindow)));
            Assert.AreEqual(1, environment.Provider.ReleaseCount);
        }

        /// <summary>
        /// Verifies that a fully released type can be registered again with a hot-update path.
        /// </summary>
        [Test]
        public void WindowDefinition_CanUseNewPathAfterFinalOwnerExits()
        {
            using UITestEnvironment environment = new();
            var firstScope = environment.UIService.CreateScope();
            firstScope.AddWindow(typeof(UITestWindow),
                                 "UI/TestWindowV1",
                                 EUILayer.Normal,
                                 EUIWindowLifetime.Module);
            environment.UIService.OpenWindow(typeof(UITestWindow));
            firstScope.Dispose();

            var secondScope = environment.UIService.CreateScope();
            secondScope.AddWindow(typeof(UITestWindow),
                                  "UI/TestWindowV2",
                                  EUILayer.Popup,
                                  EUIWindowLifetime.Module);
            environment.Root.Popup = environment.Root.Normal;
            environment.UIService.OpenWindow(typeof(UITestWindow));

            Assert.AreEqual(2, environment.Provider.LoadCount);
            Assert.AreEqual(1, environment.Provider.ReleaseCount);
            secondScope.Dispose();
            Assert.AreEqual(2, environment.Provider.ReleaseCount);
        }

        /// <summary>
        /// Verifies that active modules cannot silently register conflicting definitions.
        /// </summary>
        [Test]
        public void WindowDefinition_RejectsConflictingActiveRegistration()
        {
            using UITestEnvironment environment = new();
            var firstScope  = environment.UIService.CreateScope();
            var secondScope = environment.UIService.CreateScope();
            firstScope.AddWindow(typeof(UITestWindow),
                                 "UI/TestWindow",
                                 EUILayer.Normal,
                                 EUIWindowLifetime.Module);

            Assert.Throws<InvalidOperationException>(() =>
                secondScope.AddWindow(typeof(UITestWindow),
                                      "UI/ConflictingWindow",
                                      EUILayer.Normal,
                                      EUIWindowLifetime.Module));

            secondScope.Dispose();
            firstScope.Dispose();
        }

        /// <summary>
        /// Verifies that concurrent asynchronous opens share one provider request and instance.
        /// </summary>
        [Test]
        public void OpenWindowAsync_MergesConcurrentRequests()
        {
            using UITestEnvironment environment = new(true);
            var scope = environment.UIService.CreateScope();
            scope.AddWindow(typeof(UITestWindow),
                            "UI/AsyncWindow",
                            EUILayer.Normal,
                            EUIWindowLifetime.Module);

            var firstTask  = environment.UIService.OpenWindowAsync(typeof(UITestWindow));
            var secondTask = environment.UIService.OpenWindowAsync(typeof(UITestWindow));
            Assert.AreEqual(1, environment.Provider.LoadAsyncCount);

            environment.Provider.CompleteAsync("UI/AsyncWindow");
            var first  = firstTask.GetAwaiter().GetResult();
            var second = secondTask.GetAwaiter().GetResult();

            Assert.AreSame(first, second);
            scope.Dispose();
            Assert.AreEqual(1, environment.Provider.ReleaseCount);
        }
    }

    /// <summary>
    /// Owns one isolated UI application and all Unity objects created for a test.
    /// </summary>
    internal sealed class UITestEnvironment : IDisposable
    {
        // Stores the application under test.
        internal readonly GameApplication Application;

        // Stores the deterministic asset provider.
        internal readonly UIAssetProvider Provider;

        // Stores the UI service under test.
        internal readonly IUIService UIService;

        // Stores the configured UI root.
        internal readonly UIRoot Root;

        // Stores the source window prefab.
        private readonly GameObject mPrefabObject;

        // Stores the UI root GameObject.
        private readonly GameObject mRootObject;

        /// <summary>
        /// Creates and starts one isolated asset and UI application.
        /// </summary>
        internal UITestEnvironment(bool delayAsyncLoads = false)
        {
            mPrefabObject = new("UITestWindowPrefab");
            mPrefabObject.SetActive(false);
            mPrefabObject.AddComponent<UITestView>();
            mPrefabObject.AddComponent<UITestWindow>();

            mRootObject = new("UITestRoot");
            Root        = mRootObject.AddComponent<UIRoot>();
            GameObject normalObject = new("Normal", typeof(RectTransform));
            normalObject.transform.SetParent(mRootObject.transform, false);
            Root.Normal = normalObject.GetComponent<RectTransform>();

            Provider = new(mPrefabObject, delayAsyncLoads);
            GameApplicationBuilder builder = new();
            Application = builder
                .UseAssets(Provider)
                .UseUI(Root)
                .Build();
            Application.Start();
            UIService = Application.Services.GetRequired<IUIService>();
        }

        /// <summary>
        /// Stops the application and destroys all source test objects.
        /// </summary>
        public void Dispose()
        {
            Application.Stop();
            Object.DestroyImmediate(mRootObject);
            Object.DestroyImmediate(mPrefabObject);
        }
    }

    /// <summary>
    /// Provides deterministic GameObject assets for UI loading tests.
    /// </summary>
    internal sealed class UIAssetProvider : IAssetProvider
    {
        // Stores the prefab returned for every configured test path.
        private readonly GameObject mPrefab;

        // Stores incomplete asynchronous requests by path.
        private readonly Dictionary<string, TaskCompletionSource<object>> mPendingLoads = new(StringComparer.Ordinal);

        // Indicates whether asynchronous requests wait for explicit completion.
        private readonly bool mDelayAsyncLoads;

        // Gets the number of synchronous provider loads.
        internal int LoadCount { get; private set; }

        // Gets the number of asynchronous provider loads.
        internal int LoadAsyncCount { get; private set; }

        // Gets the number of provider releases.
        internal int ReleaseCount { get; private set; }

        /// <summary>
        /// Initializes one provider returning a stable window prefab.
        /// </summary>
        internal UIAssetProvider(GameObject prefab, bool delayAsyncLoads)
        {
            mPrefab          = prefab;
            mDelayAsyncLoads = delayAsyncLoads;
        }

        /// <inheritdoc />
        public object Load(string path, Type assetType)
        {
            LoadCount++;
            return mPrefab;
        }

        /// <inheritdoc />
        public Task<object> LoadAsync(string path, Type assetType)
        {
            LoadAsyncCount++;
            if (!mDelayAsyncLoads)
                return Task.FromResult((object)mPrefab);

            TaskCompletionSource<object> completion = new();
            mPendingLoads.Add(path, completion);
            return completion.Task;
        }

        /// <inheritdoc />
        public void Release(object asset)
        {
            ReleaseCount++;
        }

        /// <summary>
        /// Completes one delayed provider request.
        /// </summary>
        internal void CompleteAsync(string path)
        {
            var completion = mPendingLoads[path];
            mPendingLoads.Remove(path);
            completion.SetResult(mPrefab);
        }
    }

    /// <summary>
    /// Provides an empty strongly typed view for UI tests.
    /// </summary>
    public sealed class UITestView : UIView
    {
    }

    /// <summary>
    /// Provides one concrete cached window for UI tests.
    /// </summary>
    public sealed class UITestWindow : UIWindow<UITestView>
    {
    }
}
