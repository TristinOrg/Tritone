using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Assets;
using Tritone.Kernel;
using Tritone.UI;
using Tritone.Unity.Assets;
using Tritone.Unity.Pooling;
using Tritone.Unity.UI;
using UnityEngine;
using UnityEngine.UI;
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

        /// <summary>
        /// Verifies that preprocessed Canvas and Renderer nodes receive consecutive runtime orders.
        /// </summary>
        [Test]
        public void UIView_AppliesPreprocessedSortingOrder()
        {
            GameObject rootObject     = new("SortingView", typeof(RectTransform), typeof(Canvas), typeof(SortingTestView));
            GameObject canvasObject   = new("Canvas", typeof(RectTransform), typeof(Canvas));
            GameObject rendererObject = new("Renderer", typeof(SpriteRenderer));
            canvasObject.transform.SetParent(rootObject.transform, false);
            rendererObject.transform.SetParent(rootObject.transform, false);
            var view     = rootObject.GetComponent<SortingTestView>();
            var canvas   = canvasObject.GetComponent<Canvas>();
            var renderer = rendererObject.GetComponent<SpriteRenderer>();
            view.SortingNodes = new[]
            {
                new UISortingNode { Target = canvas, RelativeOrder = 0, HierarchyDepth = 1 },
                new UISortingNode { Target = renderer, RelativeOrder = 1, HierarchyDepth = 1 }
            };

            var order = 42;
            view.ApplySortingOrder(ref order);

            Assert.IsTrue(canvas.overrideSorting);
            Assert.AreEqual(42, canvas.sortingOrder);
            Assert.AreEqual(43, renderer.sortingOrder);
            Assert.AreEqual(44, order);
            Object.DestroyImmediate(rootObject);
        }

        /// <summary>
        /// Verifies that dynamically appended child views continue the owning view's sorting sequence.
        /// </summary>
        [Test]
        public void UIView_AppendsDynamicChildSortingOrder()
        {
            GameObject rootObject  = new("RootView", typeof(RectTransform), typeof(SortingTestView));
            GameObject rootCanvas  = new("RootCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject childObject = new("ChildView", typeof(RectTransform), typeof(SortingTestView));
            GameObject childCanvas = new("ChildCanvas", typeof(RectTransform), typeof(Canvas));
            rootCanvas.transform.SetParent(rootObject.transform, false);
            childObject.transform.SetParent(rootObject.transform, false);
            childCanvas.transform.SetParent(childObject.transform, false);
            var rootView  = rootObject.GetComponent<SortingTestView>();
            var childView = childObject.GetComponent<SortingTestView>();
            rootView.SortingNodes  = new[] { new UISortingNode { Target = rootCanvas.GetComponent<Canvas>() } };
            childView.SortingNodes = new[] { new UISortingNode { Target = childCanvas.GetComponent<Canvas>() } };

            var order = 100;
            rootView.ApplySortingOrder(ref order);
            rootView.AddSubView(childView);

            Assert.AreEqual(100, rootCanvas.GetComponent<Canvas>().sortingOrder);
            Assert.AreEqual(101, childCanvas.GetComponent<Canvas>().sortingOrder);
            Object.DestroyImmediate(rootObject);
        }

        /// <summary>
        /// Verifies window-owned items use pools while panels retain one instance per window activity.
        /// </summary>
        [Test]
        public void WindowComposition_ReusesItemsAndPanels()
        {
            using UITestEnvironment environment = new();
            GameObject itemPrefab = new("UITestItemPrefab");
            itemPrefab.SetActive(false);
            itemPrefab.AddComponent<UITestItemView>();
            itemPrefab.AddComponent<UITestItem>();
            Assert.IsNotNull(itemPrefab.GetComponent<UITestItem>());
            GameObject panelPrefab = new("UITestPanelPrefab");
            panelPrefab.SetActive(false);
            panelPrefab.AddComponent<UITestPanelView>();
            panelPrefab.AddComponent<UITestPanel>();
            environment.Provider.AddPrefab("UI/TestItem", itemPrefab);
            environment.Provider.AddPrefab("UI/TestPanel", panelPrefab);
            var scope = environment.UIService.CreateScope();
            scope.AddWindow(typeof(UITestWindow), "UI/TestWindow", EUILayer.Normal, EUIWindowLifetime.Module);
            var window = (UITestWindow)environment.UIService.OpenWindow(typeof(UITestWindow));
            window.ConfigureTestComposition();

            var firstItem       = window.CreateTestItem(window.transform);
            var firstItemObject = firstItem.gameObject;
            Assert.IsTrue(window.ReleaseTestItem(ref firstItem));
            var secondItem = window.CreateTestItem(window.transform);
            var firstPanel = window.OpenTestPanel();
            var secondPanel = window.OpenTestPanel();

            Assert.AreSame(firstItemObject, secondItem.gameObject);
            Assert.AreSame(firstPanel, secondPanel);
            Assert.IsTrue(window.CloseTestPanel());
            Assert.IsFalse(firstPanel.gameObject.activeSelf);
            Assert.AreEqual(3, environment.Provider.LoadCount);

            environment.UIService.CloseWindow(typeof(UITestWindow));
            Assert.IsFalse(secondItem.gameObject.activeInHierarchy);
            scope.Dispose();
            Assert.AreEqual(3, environment.Provider.ReleaseCount);
            Object.DestroyImmediate(itemPrefab);
            Object.DestroyImmediate(panelPrefab);
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
                .UsePools()
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

        /// <summary>
        /// Stores path-specific prefabs used by composition tests.
        /// </summary>
        private readonly Dictionary<string, GameObject> mPrefabs = new(StringComparer.Ordinal);

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
            return mPrefabs.TryGetValue(path, out var prefab) ? prefab : mPrefab;
        }

        /// <inheritdoc />
        public Task<object> LoadAsync(string path, Type assetType)
        {
            LoadAsyncCount++;
            if (!mDelayAsyncLoads)
                return Task.FromResult((object)GetPrefab(path));

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
            completion.SetResult(GetPrefab(path));
        }

        /// <summary>
        /// Registers one path-specific prefab.
        /// </summary>
        /// <param name="path">The provider path.</param>
        /// <param name="prefab">The prefab returned for the path.</param>
        internal void AddPrefab(string path, GameObject prefab)
        {
            mPrefabs.Add(path, prefab);
        }

        /// <summary>
        /// Gets a path-specific prefab or the default window prefab.
        /// </summary>
        /// <param name="path">The requested provider path.</param>
        /// <returns>The configured prefab.</returns>
        private GameObject GetPrefab(string path)
        {
            return mPrefabs.TryGetValue(path, out var prefab) ? prefab : mPrefab;
        }
    }

    /// <summary>
    /// Provides an empty strongly typed view for UI tests.
    /// </summary>
    public sealed class UITestView : UIView
    {
    }

    /// <summary>
    /// Provides a view used to verify preprocessed sorting metadata.
    /// </summary>
    public sealed class SortingTestView : UIView
    {
    }

    /// <summary>
    /// Provides one concrete cached window for UI tests.
    /// </summary>
    public sealed class UITestWindow : UIWindow<UITestView>
    {
        /// <inheritdoc />
        protected override void OnInitialize()
        {
            ConfigureTestComposition();
        }

        /// <summary>
        /// Registers composition explicitly when EditMode does not invoke MonoBehaviour initialization.
        /// </summary>
        internal void ConfigureTestComposition()
        {
            AddItemTemplate<UITestItem>("UI/TestItem");
            AddPanel<UITestPanel>("UI/TestPanel");
        }

        /// <summary>
        /// Creates one test item.
        /// </summary>
        /// <param name="parent">The item parent.</param>
        /// <returns>The created item.</returns>
        internal UITestItem CreateTestItem(Transform parent)
        {
            return CreateItem<UITestItem>(parent);
        }

        /// <summary>
        /// Releases one test item.
        /// </summary>
        /// <param name="item">The item to release and clear.</param>
        /// <returns>True when the item was released.</returns>
        internal bool ReleaseTestItem(ref UITestItem item)
        {
            return ReleaseItem(ref item);
        }

        /// <summary>
        /// Opens the test panel.
        /// </summary>
        /// <returns>The test panel.</returns>
        internal UITestPanel OpenTestPanel()
        {
            return OpenPanel<UITestPanel>();
        }

        /// <summary>
        /// Closes the test panel.
        /// </summary>
        /// <returns>True when the panel was closed.</returns>
        internal bool CloseTestPanel()
        {
            return ClosePanel<UITestPanel>();
        }
    }

    /// <summary>
    /// Provides an empty view for a reusable test item.
    /// </summary>
    public sealed class UITestItemView : UIView
    {
    }

    /// <summary>
    /// Provides one concrete reusable test item.
    /// </summary>
    public sealed class UITestItem : UIItem<UITestItemView>
    {
    }

    /// <summary>
    /// Provides an empty view for a test panel.
    /// </summary>
    public sealed class UITestPanelView : UIView
    {
    }

    /// <summary>
    /// Provides one concrete single-instance test panel.
    /// </summary>
    public sealed class UITestPanel : UIPanel<UITestPanelView>
    {
    }

}
