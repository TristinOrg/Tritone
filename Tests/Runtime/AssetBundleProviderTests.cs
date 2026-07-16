using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Unity.Assets.AssetBundles;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies AssetBundle dependency order, request sharing, references, and validation.
    /// </summary>
    public sealed class AssetBundleProviderTests
    {
        /// <summary>
        /// Verifies that assets share bundle dependencies until the final release.
        /// </summary>
        [Test]
        public void Load_UsesDependenciesAndUnloadsAfterFinalAsset()
        {
            var registry = CreateRegistry();
            FakeBundleSource source = new();
            source.AddAsset("ui", "Assets/UI/WindowA.prefab", new BundleTestAsset("A"));
            source.AddAsset("ui", "Assets/UI/WindowB.prefab", new BundleTestAsset("B"));
            AssetBundleAssetProvider provider = new(registry, source);

            var first  = provider.Load("UI/WindowA", typeof(BundleTestAsset));
            var second = provider.Load("UI/WindowB", typeof(BundleTestAsset));

            CollectionAssert.AreEqual(new[] { "core", "shared", "ui" }, source.SyncLoadOrder);
            Assert.AreEqual(1, source.GetLoadCount("core"));
            Assert.AreEqual(1, source.GetLoadCount("shared"));
            Assert.AreEqual(1, source.GetLoadCount("ui"));

            provider.Release(first);
            Assert.AreEqual(0, source.UnloadOrder.Count);

            provider.Release(second);
            CollectionAssert.AreEqual(new[] { "ui", "shared", "core" }, source.UnloadOrder);
        }

        /// <summary>
        /// Verifies that concurrent assets join the same asynchronous bundle operations.
        /// </summary>
        [Test]
        public void LoadAsync_MergesConcurrentBundleRequests()
        {
            var registry = CreateRegistry();
            FakeBundleSource source = new(true);
            source.AddAsset("ui", "Assets/UI/WindowA.prefab", new BundleTestAsset("A"));
            source.AddAsset("ui", "Assets/UI/WindowB.prefab", new BundleTestAsset("B"));
            AssetBundleAssetProvider provider = new(registry, source);

            var firstTask  = provider.LoadAsync("UI/WindowA", typeof(BundleTestAsset));
            var secondTask = provider.LoadAsync("UI/WindowB", typeof(BundleTestAsset));
            Assert.AreEqual(1, source.GetAsyncLoadCount("core"));

            source.CompleteBundle("core");
            Assert.AreEqual(1, source.GetAsyncLoadCount("shared"));
            source.CompleteBundle("shared");
            Assert.AreEqual(1, source.GetAsyncLoadCount("ui"));
            source.CompleteBundle("ui");

            var first  = firstTask.GetAwaiter().GetResult();
            var second = secondTask.GetAwaiter().GetResult();
            Assert.AreEqual("A", ((BundleTestAsset)first).Name);
            Assert.AreEqual("B", ((BundleTestAsset)second).Name);

            provider.Release(first);
            provider.Release(second);
            CollectionAssert.AreEqual(new[] { "ui", "shared", "core" }, source.UnloadOrder);
        }

        /// <summary>
        /// Verifies that a failed asset load rolls back every acquired bundle reference.
        /// </summary>
        [Test]
        public void Load_MissingAssetRollsBackBundles()
        {
            var registry = CreateRegistry();
            FakeBundleSource source = new();
            AssetBundleAssetProvider provider = new(registry, source);

            Assert.Throws<InvalidOperationException>(() =>
                provider.Load("UI/WindowA", typeof(BundleTestAsset)));
            CollectionAssert.AreEqual(new[] { "ui", "shared", "core" }, source.UnloadOrder);
        }

        /// <summary>
        /// Verifies that dependency cycles are rejected before any loading starts.
        /// </summary>
        [Test]
        public void Constructor_RejectsDependencyCycle()
        {
            AssetBundleRegistry registry = new();
            registry.AddBundle("a", "a.bundle", "b")
                    .AddBundle("b", "b.bundle", "a")
                    .AddAsset("Data/Test", "a", "Assets/Test.asset");

            Assert.Throws<InvalidOperationException>(() =>
                new AssetBundleAssetProvider(registry, new FakeBundleSource()));
        }

        /// <summary>
        /// Creates one registry with a three-level dependency chain and two UI assets.
        /// </summary>
        private static AssetBundleRegistry CreateRegistry()
        {
            AssetBundleRegistry registry = new();
            registry.AddBundle("core", "core.bundle")
                    .AddBundle("shared", "shared.bundle", "core")
                    .AddBundle("ui", "ui.bundle", "shared")
                    .AddAsset("UI/WindowA", "ui", "Assets/UI/WindowA.prefab")
                    .AddAsset("UI/WindowB", "ui", "Assets/UI/WindowB.prefab");
            return registry;
        }
    }

    /// <summary>
    /// Provides deterministic bundle handles and delayed requests for provider tests.
    /// </summary>
    internal sealed class FakeBundleSource : IAssetBundleSource
    {
        /// <summary>
        /// Stores handles by logical bundle name.
        /// </summary>
        private readonly Dictionary<string, FakeBundleHandle> mHandles = new(StringComparer.Ordinal);

        /// <summary>
        /// Stores synchronous load counts by logical bundle name.
        /// </summary>
        private readonly Dictionary<string, int> mSyncLoadCounts = new(StringComparer.Ordinal);

        /// <summary>
        /// Stores asynchronous load counts by logical bundle name.
        /// </summary>
        private readonly Dictionary<string, int> mAsyncLoadCounts = new(StringComparer.Ordinal);

        /// <summary>
        /// Stores delayed asynchronous operations by logical bundle name.
        /// </summary>
        private readonly Dictionary<string, TaskCompletionSource<IAssetBundleHandle>> mPendingLoads = new(StringComparer.Ordinal);

        /// <summary>
        /// Indicates whether asynchronous bundle loads require explicit completion.
        /// </summary>
        private readonly bool mDelayAsyncLoads;

        /// <summary>
        /// Stores the order of unique synchronous source loads.
        /// </summary>
        internal readonly List<string> SyncLoadOrder = new();

        /// <summary>
        /// Stores the order in which final bundle references unload.
        /// </summary>
        internal readonly List<string> UnloadOrder = new();

        /// <summary>
        /// Initializes one fake source with optional delayed asynchronous loading.
        /// </summary>
        internal FakeBundleSource(bool delayAsyncLoads = false)
        {
            mDelayAsyncLoads = delayAsyncLoads;
        }

        /// <inheritdoc />
        public IAssetBundleHandle LoadBundle(string bundleName, string fileName)
        {
            IncreaseCount(mSyncLoadCounts, bundleName);
            SyncLoadOrder.Add(bundleName);
            return GetHandle(bundleName);
        }

        /// <inheritdoc />
        public Task<IAssetBundleHandle> LoadBundleAsync(string bundleName, string fileName)
        {
            IncreaseCount(mAsyncLoadCounts, bundleName);
            if (!mDelayAsyncLoads)
                return Task.FromResult<IAssetBundleHandle>(GetHandle(bundleName));

            TaskCompletionSource<IAssetBundleHandle> completion = new();
            mPendingLoads.Add(bundleName, completion);
            return completion.Task;
        }

        /// <summary>
        /// Adds one asset to a logical fake bundle.
        /// </summary>
        internal void AddAsset(string bundleName, string assetName, object asset)
        {
            GetHandle(bundleName).AddAsset(assetName, asset);
        }

        /// <summary>
        /// Completes one delayed logical bundle operation.
        /// </summary>
        internal void CompleteBundle(string bundleName)
        {
            var completion = mPendingLoads[bundleName];
            mPendingLoads.Remove(bundleName);
            completion.SetResult(GetHandle(bundleName));
        }

        /// <summary>
        /// Gets one synchronous source load count.
        /// </summary>
        internal int GetLoadCount(string bundleName)
        {
            return mSyncLoadCounts.TryGetValue(bundleName, out var count) ? count : 0;
        }

        /// <summary>
        /// Gets one asynchronous source load count.
        /// </summary>
        internal int GetAsyncLoadCount(string bundleName)
        {
            return mAsyncLoadCounts.TryGetValue(bundleName, out var count) ? count : 0;
        }

        /// <summary>
        /// Gets or creates one stable fake bundle handle.
        /// </summary>
        private FakeBundleHandle GetHandle(string bundleName)
        {
            if (mHandles.TryGetValue(bundleName, out var handle))
                return handle;

            handle = new(bundleName, UnloadOrder);
            mHandles.Add(bundleName, handle);
            return handle;
        }

        /// <summary>
        /// Increments one dictionary count without allocating lookup helpers.
        /// </summary>
        private static void IncreaseCount(Dictionary<string, int> counts, string bundleName)
        {
            counts.TryGetValue(bundleName, out var count);
            counts[bundleName] = count + 1;
        }
    }

    /// <summary>
    /// Provides deterministic asset access and unload reporting for one fake bundle.
    /// </summary>
    internal sealed class FakeBundleHandle : IAssetBundleHandle
    {
        /// <summary>
        /// Stores the logical bundle name.
        /// </summary>
        private readonly string mBundleName;

        /// <summary>
        /// Stores asset objects by exact internal name.
        /// </summary>
        private readonly Dictionary<string, object> mAssets = new(StringComparer.Ordinal);

        /// <summary>
        /// Stores the shared final unload order.
        /// </summary>
        private readonly List<string> mUnloadOrder;

        /// <summary>
        /// Initializes one fake bundle handle.
        /// </summary>
        internal FakeBundleHandle(string bundleName, List<string> unloadOrder)
        {
            mBundleName  = bundleName;
            mUnloadOrder = unloadOrder;
        }

        /// <inheritdoc />
        public object LoadAsset(string assetName, Type assetType)
        {
            return mAssets.TryGetValue(assetName, out var asset) && assetType.IsInstanceOfType(asset)
                ? asset
                : null;
        }

        /// <inheritdoc />
        public Task<object> LoadAssetAsync(string assetName, Type assetType)
        {
            return Task.FromResult(LoadAsset(assetName, assetType));
        }

        /// <inheritdoc />
        public void Unload(bool unloadAllLoadedObjects)
        {
            mUnloadOrder.Add(mBundleName);
        }

        /// <summary>
        /// Adds or replaces one exact internal asset name.
        /// </summary>
        internal void AddAsset(string assetName, object asset)
        {
            mAssets[assetName] = asset;
        }
    }

    /// <summary>
    /// Stores one named reference value loaded by fake bundles.
    /// </summary>
    internal sealed class BundleTestAsset
    {
        /// <summary>
        /// Gets the test asset name.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Initializes one named test asset.
        /// </summary>
        internal BundleTestAsset(string name)
        {
            Name = name;
        }
    }
}
