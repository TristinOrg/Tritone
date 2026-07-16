using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Assets;
using Tritone.Kernel;
using Tritone.Unity.Assets;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies asset caching, shared requests, reference counts, and automatic scope cleanup.
    /// </summary>
    public sealed class AssetTests
    {
        /// <summary>
        /// Verifies that repeated synchronous requests reuse one provider load.
        /// </summary>
        [Test]
        public void LoadAsset_ReusesCachedAssetUntilLastReferenceIsReleased()
        {
            TestAssetProvider provider = new();
            var application            = CreateApplication(provider, out var consumer);

            var first  = consumer.LoadData("Data/Player");
            var second = consumer.LoadData("Data/Player");

            Assert.AreSame(first, second);
            Assert.AreEqual(1, provider.LoadCount);
            Assert.IsTrue(consumer.ReleaseData(first));
            Assert.AreEqual(0, provider.ReleaseCount);

            application.Stop();
            Assert.AreEqual(1, provider.ReleaseCount);
        }

        /// <summary>
        /// Verifies that concurrent asynchronous requests share one provider operation.
        /// </summary>
        [Test]
        public void LoadAssetAsync_MergesConcurrentRequests()
        {
            TestAssetProvider provider = new(true);
            var application            = CreateApplication(provider, out var consumer);

            var firstTask  = consumer.LoadDataAsync("Data/Shared");
            var secondTask = consumer.LoadDataAsync("Data/Shared");
            Assert.AreEqual(1, provider.LoadAsyncCount);

            provider.CompleteAsync("Data/Shared");
            var first  = firstTask.GetAwaiter().GetResult();
            var second = secondTask.GetAwaiter().GetResult();

            Assert.AreSame(first, second);
            application.Stop();
            Assert.AreEqual(1, provider.ReleaseCount);
        }

        /// <summary>
        /// Verifies that stopping a module releases assets it did not release manually.
        /// </summary>
        [Test]
        public void Stop_AutomaticallyReleasesOwnedAssets()
        {
            TestAssetProvider provider = new();
            var application            = CreateApplication(provider, out var consumer);

            consumer.LoadData("Data/Automatic");
            application.Stop();

            Assert.AreEqual(1, provider.ReleaseCount);
        }

        /// <summary>
        /// Verifies that a disposed waiter cannot release an asset still needed by another scope.
        /// </summary>
        [Test]
        public void LoadAssetAsync_KeepsSharedRequestAliveForRemainingScope()
        {
            TestAssetProvider provider    = new(true);
            GameApplicationBuilder builder = new();
            var application                = builder.UseAssets(provider).Build();
            application.Start();

            var service     = application.Services.GetRequired<IAssetService>();
            var firstScope  = service.CreateScope();
            var secondScope = service.CreateScope();
            var firstTask   = firstScope.LoadAsync<AssetTestData>("Data/Lifetime");
            var secondTask  = secondScope.LoadAsync<AssetTestData>("Data/Lifetime");

            firstScope.Dispose();
            provider.CompleteAsync("Data/Lifetime");

            Assert.Throws<ObjectDisposedException>(() => firstTask.GetAwaiter().GetResult());
            Assert.IsNotNull(secondTask.GetAwaiter().GetResult());
            Assert.AreEqual(0, provider.ReleaseCount);

            secondScope.Dispose();
            Assert.AreEqual(1, provider.ReleaseCount);
            application.Stop();
        }

        /// <summary>
        /// Creates one application containing asset infrastructure and a consumer module.
        /// </summary>
        private static GameApplication CreateApplication(TestAssetProvider provider,
                                                         out AssetConsumerModule consumer)
        {
            consumer = new();
            GameApplicationBuilder builder = new();
            var application                = builder.UseAssets(provider)
                .AddModule(consumer)
                .Build();
            application.Start();
            return application;
        }

        /// <summary>
        /// Exposes protected ModuleBase asset helpers for tests.
        /// </summary>
        private sealed class AssetConsumerModule : ModuleBase
        {
            /// <summary>
            /// Loads one test asset synchronously.
            /// </summary>
            internal AssetTestData LoadData(string path)
            {
                return LoadAsset<AssetTestData>(path);
            }

            /// <summary>
            /// Loads one test asset asynchronously.
            /// </summary>
            internal Task<AssetTestData> LoadDataAsync(string path)
            {
                return LoadAssetAsync<AssetTestData>(path);
            }

            /// <summary>
            /// Releases one test asset reference.
            /// </summary>
            internal bool ReleaseData(AssetTestData asset)
            {
                return ReleaseAsset(asset);
            }
        }

        /// <summary>
        /// Provides deterministic synchronous and asynchronous assets for tests.
        /// </summary>
        private sealed class TestAssetProvider : IAssetProvider
        {
            // Stores reusable assets by their provider path.
            private readonly Dictionary<string, AssetTestData> mAssets = new(StringComparer.Ordinal);

            // Stores incomplete asynchronous operations by their provider path.
            private readonly Dictionary<string, TaskCompletionSource<object>> mPendingLoads = new(StringComparer.Ordinal);

            // Indicates whether asynchronous requests wait for explicit completion.
            private readonly bool mDelayAsyncLoads;

            // Gets the number of synchronous provider loads.
            internal int LoadCount { get; private set; }

            // Gets the number of asynchronous provider loads.
            internal int LoadAsyncCount { get; private set; }

            // Gets the number of zero-reference provider releases.
            internal int ReleaseCount { get; private set; }

            /// <summary>
            /// Initializes a provider with optional delayed asynchronous completion.
            /// </summary>
            internal TestAssetProvider(bool delayAsyncLoads = false)
            {
                mDelayAsyncLoads = delayAsyncLoads;
            }

            /// <inheritdoc />
            public object Load(string path, Type assetType)
            {
                LoadCount++;
                return GetAsset(path);
            }

            /// <inheritdoc />
            public Task<object> LoadAsync(string path, Type assetType)
            {
                LoadAsyncCount++;
                if (!mDelayAsyncLoads)
                    return Task.FromResult((object)GetAsset(path));

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
            /// Completes one delayed asynchronous request.
            /// </summary>
            internal void CompleteAsync(string path)
            {
                var completion = mPendingLoads[path];
                mPendingLoads.Remove(path);
                completion.SetResult(GetAsset(path));
            }

            /// <summary>
            /// Gets or creates one stable asset for a path.
            /// </summary>
            private AssetTestData GetAsset(string path)
            {
                if (mAssets.TryGetValue(path, out var asset))
                    return asset;

                asset = new(path);
                mAssets.Add(path, asset);
                return asset;
            }
        }
    }

    /// <summary>
    /// Stores the provider path used by one asset test value.
    /// </summary>
    public sealed class AssetTestData
    {
        // Gets the provider path represented by this value.
        public string Path { get; }

        /// <summary>
        /// Initializes one test asset value.
        /// </summary>
        public AssetTestData(string path)
        {
            Path = path;
        }
    }
}
