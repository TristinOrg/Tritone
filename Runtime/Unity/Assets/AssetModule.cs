using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tritone.Assets;
using Tritone.Kernel;
using Object = UnityEngine.Object;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Merges asset requests, caches active assets, and manages shared reference counts.
    /// </summary>
    public sealed class AssetModule : ModuleBase, IAssetService
    {
        // Stores the concrete resource backend.
        private readonly IAssetProvider mProvider;

        // Maps each path and type pair to its shared load state.
        private readonly Dictionary<AssetKey, AssetEntry> mEntries = new();

        // Indicates whether this module has permanently stopped.
        private bool mStopped;

        /// <summary>
        /// Initializes asset management with one concrete provider.
        /// </summary>
        public AssetModule(IAssetProvider provider)
        {
            mProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Registers application-wide asset access.
        /// </summary>
        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton<IAssetService>(this);
        }

        /// <inheritdoc />
        public IAssetScope CreateScope()
        {
            if (mStopped)
                throw new InvalidOperationException("Asset scopes cannot be created after the asset module has stopped.");

            return new AssetScope(this);
        }

        /// <summary>
        /// Loads or reuses one asset for a specific ownership scope.
        /// </summary>
        internal T Load<T>(AssetScope owner, string path) where T : class
        {
            ValidatePath(path);
            ThrowIfStopped();

            AssetKey key = new(path, typeof(T));
            if (!mEntries.TryGetValue(key, out var entry))
            {
                entry = new();
                mEntries.Add(key, entry);
            }
            else if (entry.Asset != null)
                return Acquire<T>(owner, key, entry, entry.Asset);
            else if (entry.LoadTask != null)
                throw new InvalidOperationException($"Asset '{path}' is already loading asynchronously and cannot be loaded synchronously.");

            object asset;
            try
            {
                asset = mProvider.Load(path, typeof(T));
                ValidateAsset<T>(path, asset);
                entry.Asset = asset;
            }
            catch
            {
                mEntries.Remove(key);
                throw;
            }

            return Acquire<T>(owner, key, entry, asset);
        }

        /// <summary>
        /// Loads or joins one asynchronous request for a specific ownership scope.
        /// </summary>
        internal async Task<T> LoadAsync<T>(AssetScope owner, string path) where T : class
        {
            ValidatePath(path);
            ThrowIfStopped();

            AssetKey key = new(path, typeof(T));
            if (!mEntries.TryGetValue(key, out var entry))
            {
                entry = new();
                mEntries.Add(key, entry);
            }
            if (entry.Asset != null)
                return Acquire<T>(owner, key, entry, entry.Asset);

            Task<object> loadTask;
            entry.PendingCount++;
            try
            {
                entry.LoadTask ??= mProvider.LoadAsync(path, typeof(T)) ??
                                   throw new InvalidOperationException("The asset provider returned a null loading task.");
                loadTask = entry.LoadTask;
                var asset = await loadTask;
                ValidateAsset<T>(path, asset);

                if (mStopped)
                {
                    ReleaseProviderAsset(entry, asset);
                    throw new ObjectDisposedException(nameof(AssetModule));
                }
                if (entry.Asset == null)
                {
                    entry.Asset    = asset;
                    entry.LoadTask = null;
                }
                else if (!ReferenceEquals(entry.Asset, asset))
                    throw new InvalidOperationException($"The provider returned different assets for the shared request '{path}'.");

                return Acquire<T>(owner, key, entry, entry.Asset);
            }
            finally
            {
                entry.PendingCount--;
                if (!mStopped)
                    TryReleaseUnused(key, entry);
            }
        }

        /// <summary>
        /// Releases one shared reference and returns zero-reference assets to the provider.
        /// </summary>
        internal void Release(AssetKey key)
        {
            if (!mEntries.TryGetValue(key, out var entry) || entry.ReferenceCount < 1)
                return;

            entry.ReferenceCount--;
            TryReleaseUnused(key, entry);
        }

        /// <summary>
        /// Releases every cached asset reference when the application stops.
        /// </summary>
        protected override void OnStop()
        {
            mStopped = true;
            foreach (var pair in mEntries)
            {
                if (pair.Value.Asset != null)
                    ReleaseProviderAsset(pair.Value, pair.Value.Asset);
            }
            mEntries.Clear();
        }

        /// <summary>
        /// Adds one reference and records it in the owning scope.
        /// </summary>
        private T Acquire<T>(AssetScope owner, AssetKey key, AssetEntry entry, object asset) where T : class
        {
            entry.ReferenceCount++;
            if (owner.Track(key, asset))
                return (T)asset;

            Release(key);
            throw new ObjectDisposedException(nameof(AssetScope));
        }

        /// <summary>
        /// Removes and releases an entry after its final reference and waiter are gone.
        /// </summary>
        private void TryReleaseUnused(AssetKey key, AssetEntry entry)
        {
            if (entry.ReferenceCount > 0 || entry.PendingCount > 0)
                return;
            if (!mEntries.TryGetValue(key, out var currentEntry) || !ReferenceEquals(currentEntry, entry))
                return;

            mEntries.Remove(key);
            if (entry.Asset != null)
                ReleaseProviderAsset(entry, entry.Asset);
        }

        /// <summary>
        /// Returns one provider asset at most once across shared asynchronous continuations.
        /// </summary>
        private void ReleaseProviderAsset(AssetEntry entry, object asset)
        {
            if (entry.ProviderReleased)
                return;

            entry.ProviderReleased = true;
            mProvider.Release(asset);
        }

        /// <summary>
        /// Validates that a provider result exists and matches the requested type.
        /// </summary>
        private static void ValidateAsset<T>(string path, object asset) where T : class
        {
            if (asset == null || asset is Object unityAsset && unityAsset == null)
                throw new InvalidOperationException($"Asset '{path}' of type '{typeof(T).Name}' was not found.");
            if (asset is not T)
                throw new InvalidOperationException($"Asset provider returned '{asset.GetType().Name}' for requested type '{typeof(T).Name}'.");
        }

        /// <summary>
        /// Rejects empty provider paths.
        /// </summary>
        private static void ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("An asset path cannot be null, empty, or whitespace.", nameof(path));
        }

        /// <summary>
        /// Rejects requests after application shutdown.
        /// </summary>
        private void ThrowIfStopped()
        {
            if (mStopped)
                throw new ObjectDisposedException(nameof(AssetModule));
        }
    }

    /// <summary>
    /// Stores the shared state for one cached asset request.
    /// </summary>
    internal sealed class AssetEntry
    {
        // Stores the completed provider asset.
        internal object Asset;

        // Stores one in-flight request shared by concurrent callers.
        internal Task<object> LoadTask;

        // Stores the number of active scope references.
        internal int ReferenceCount;

        // Stores the number of callers awaiting the shared provider task.
        internal int PendingCount;

        // Indicates whether the provider asset has already been released.
        internal bool ProviderReleased;
    }
}
