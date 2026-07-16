using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tritone.Assets;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Tracks asset references so its owner can release them as one lifetime.
    /// </summary>
    internal sealed class AssetScope : IAssetScope
    {
        // Stores the shared asset module.
        private readonly AssetModule mModule;

        // Stores one lightweight record for every acquired reference.
        private readonly List<AssetLease> mLeases = new();

        // Indicates whether this scope has already released its references.
        private bool mDisposed;

        /// <summary>
        /// Initializes an empty ownership scope.
        /// </summary>
        internal AssetScope(AssetModule module)
        {
            mModule = module;
        }

        /// <inheritdoc />
        public T Load<T>(string path) where T : class
        {
            ThrowIfDisposed();
            return mModule.Load<T>(this, path);
        }

        /// <inheritdoc />
        public Task<T> LoadAsync<T>(string path) where T : class
        {
            ThrowIfDisposed();
            return mModule.LoadAsync<T>(this, path);
        }

        /// <inheritdoc />
        public bool Release<T>(T asset) where T : class
        {
            if (mDisposed || asset == null)
                return false;

            for (int i = mLeases.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(mLeases[i].Asset, asset))
                    continue;

                var key       = mLeases[i].Key;
                var lastIndex = mLeases.Count - 1;
                mLeases[i]    = mLeases[lastIndex];
                mLeases.RemoveAt(lastIndex);
                mModule.Release(key);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (mDisposed)
                return;

            mDisposed = true;
            for (int i = mLeases.Count - 1; i >= 0; i--)
                mModule.Release(mLeases[i].Key);
            mLeases.Clear();
        }

        /// <summary>
        /// Records a newly acquired reference when this scope is still active.
        /// </summary>
        internal bool Track(AssetKey key, object asset)
        {
            if (mDisposed)
                return false;

            mLeases.Add(new AssetLease(key, asset));
            return true;
        }

        /// <summary>
        /// Rejects use after the owning lifetime has ended.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (mDisposed)
                throw new ObjectDisposedException(nameof(AssetScope));
        }
    }

    /// <summary>
    /// Stores one asset reference acquired by an ownership scope.
    /// </summary>
    internal readonly struct AssetLease
    {
        // Stores the cache key whose reference count must be released.
        internal readonly AssetKey Key;

        // Stores the returned object used by the convenient ReleaseAsset call.
        internal readonly object Asset;

        /// <summary>
        /// Initializes one immutable asset lease.
        /// </summary>
        internal AssetLease(AssetKey key, object asset)
        {
            Key   = key;
            Asset = asset;
        }
    }
}
