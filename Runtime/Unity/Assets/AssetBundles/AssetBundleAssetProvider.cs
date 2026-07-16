using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tritone.Assets;
using Object = UnityEngine.Object;

namespace Tritone.Unity.Assets.AssetBundles
{
    /// <summary>
    /// Loads addressed assets from dependency-aware bundles with shared requests and reference counts.
    /// </summary>
    public sealed class AssetBundleAssetProvider : IAssetProvider
    {
        /// <summary>
        /// Stores the immutable address and dependency registry.
        /// </summary>
        private readonly AssetBundleRegistrySnapshot mRegistry;

        /// <summary>
        /// Stores the local, remote, or cached source that opens bundle handles.
        /// </summary>
        private readonly IAssetBundleSource mSource;

        /// <summary>
        /// Stores loaded and in-flight bundles by logical name.
        /// </summary>
        private readonly Dictionary<string, LoadedBundle> mBundles = new(StringComparer.Ordinal);

        /// <summary>
        /// Stores bundle ownership for every asset object returned by this provider.
        /// </summary>
        private readonly Dictionary<object, List<AssetBundleLease>> mAssetLeases = new(ReferenceComparer.Instance);

        /// <summary>
        /// Controls whether final bundle unload also destroys its loaded Unity objects.
        /// </summary>
        private readonly bool mUnloadAllLoadedObjects;

        /// <summary>
        /// Initializes one dependency-aware AssetBundle provider.
        /// </summary>
        /// <param name="registry">The composed logical asset and bundle registrations.</param>
        /// <param name="source">The backend used to open bundle handles.</param>
        /// <param name="unloadAllLoadedObjects">Whether final bundle unload destroys its loaded Unity objects.</param>
        public AssetBundleAssetProvider(AssetBundleRegistry registry,
                                        IAssetBundleSource source,
                                        bool unloadAllLoadedObjects = false)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            mRegistry               = registry.CreateSnapshot();
            mSource                 = source ?? throw new ArgumentNullException(nameof(source));
            mUnloadAllLoadedObjects = unloadAllLoadedObjects;
        }

        /// <inheritdoc />
        public object Load(string path, Type assetType)
        {
            var assetDefinition = mRegistry.GetAsset(path);
            var loadOrder       = mRegistry.GetLoadOrder(assetDefinition.BundleName);
            var acquiredCount   = 0;
            try
            {
                for (int i = 0, cnt = loadOrder.Length; i < cnt; i++)
                {
                    AcquireBundle(loadOrder[i]);
                    acquiredCount++;
                }

                var rootBundle = mBundles[assetDefinition.BundleName].Handle;
                var asset      = rootBundle.LoadAsset(assetDefinition.AssetName, assetType);
                ValidateAsset(path, assetType, asset);
                TrackAsset(asset, loadOrder);
                return asset;
            }
            catch
            {
                ReleaseBundles(loadOrder, acquiredCount);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<object> LoadAsync(string path, Type assetType)
        {
            var assetDefinition = mRegistry.GetAsset(path);
            var loadOrder       = mRegistry.GetLoadOrder(assetDefinition.BundleName);
            var acquiredCount   = 0;
            try
            {
                for (int i = 0, cnt = loadOrder.Length; i < cnt; i++)
                {
                    await AcquireBundleAsync(loadOrder[i]);
                    acquiredCount++;
                }

                var rootBundle = mBundles[assetDefinition.BundleName].Handle;
                var asset      = await rootBundle.LoadAssetAsync(assetDefinition.AssetName, assetType);
                ValidateAsset(path, assetType, asset);
                TrackAsset(asset, loadOrder);
                return asset;
            }
            catch
            {
                ReleaseBundles(loadOrder, acquiredCount);
                throw;
            }
        }

        /// <inheritdoc />
        public void Release(object asset)
        {
            if (asset == null || !mAssetLeases.TryGetValue(asset, out var leases) || leases.Count == 0)
                return;

            var lastIndex = leases.Count - 1;
            var lease     = leases[lastIndex];
            leases.RemoveAt(lastIndex);
            if (leases.Count == 0)
                mAssetLeases.Remove(asset);
            ReleaseBundles(lease.LoadOrder, lease.LoadOrder.Length);
        }

        /// <summary>
        /// Acquires one synchronously loaded bundle reference.
        /// </summary>
        private void AcquireBundle(string bundleName)
        {
            if (!mBundles.TryGetValue(bundleName, out var loadedBundle))
            {
                loadedBundle = new();
                mBundles.Add(bundleName, loadedBundle);
            }
            if (loadedBundle.Handle != null)
            {
                loadedBundle.ReferenceCount++;
                return;
            }
            if (loadedBundle.LoadTask != null)
                throw new InvalidOperationException($"AssetBundle '{bundleName}' is already loading asynchronously.");

            var definition = mRegistry.GetBundle(bundleName);
            try
            {
                loadedBundle.Handle = mSource.LoadBundle(definition.BundleName, definition.FileName) ??
                                      throw new InvalidOperationException($"AssetBundle '{bundleName}' could not be loaded.");
                loadedBundle.ReferenceCount = 1;
            }
            catch
            {
                mBundles.Remove(bundleName);
                throw;
            }
        }

        /// <summary>
        /// Acquires one asynchronously loaded bundle reference and joins an existing request when possible.
        /// </summary>
        private async Task AcquireBundleAsync(string bundleName)
        {
            if (!mBundles.TryGetValue(bundleName, out var loadedBundle))
            {
                loadedBundle = new();
                mBundles.Add(bundleName, loadedBundle);
            }
            if (loadedBundle.Handle != null)
            {
                loadedBundle.ReferenceCount++;
                return;
            }

            loadedBundle.PendingCount++;
            try
            {
                var definition = mRegistry.GetBundle(bundleName);
                loadedBundle.LoadTask ??= mSource.LoadBundleAsync(definition.BundleName, definition.FileName) ??
                                         throw new InvalidOperationException("The AssetBundle source returned a null loading task.");
                var handle = await loadedBundle.LoadTask;
                if (handle == null)
                    throw new InvalidOperationException($"AssetBundle '{bundleName}' could not be loaded.");
                if (loadedBundle.Handle == null)
                {
                    loadedBundle.Handle   = handle;
                    loadedBundle.LoadTask = null;
                }
                else if (!ReferenceEquals(loadedBundle.Handle, handle))
                    throw new InvalidOperationException($"The source returned different handles for shared AssetBundle '{bundleName}'.");

                loadedBundle.ReferenceCount++;
            }
            finally
            {
                loadedBundle.PendingCount--;
                TryUnloadBundle(bundleName, loadedBundle);
            }
        }

        /// <summary>
        /// Records one provider acquisition so Release can decrement every dependency.
        /// </summary>
        private void TrackAsset(object asset, string[] loadOrder)
        {
            if (!mAssetLeases.TryGetValue(asset, out var leases))
            {
                leases = new();
                mAssetLeases.Add(asset, leases);
            }
            leases.Add(new AssetBundleLease(loadOrder));
        }

        /// <summary>
        /// Releases an acquired suffix of one dependency-first bundle order in reverse order.
        /// </summary>
        private void ReleaseBundles(string[] loadOrder, int acquiredCount)
        {
            for (int i = acquiredCount - 1; i >= 0; i--)
            {
                var bundleName = loadOrder[i];
                if (!mBundles.TryGetValue(bundleName, out var loadedBundle) || loadedBundle.ReferenceCount < 1)
                    continue;

                loadedBundle.ReferenceCount--;
                TryUnloadBundle(bundleName, loadedBundle);
            }
        }

        /// <summary>
        /// Unloads and removes one bundle after its final reference and waiter are gone.
        /// </summary>
        private void TryUnloadBundle(string bundleName, LoadedBundle loadedBundle)
        {
            if (loadedBundle.ReferenceCount > 0 || loadedBundle.PendingCount > 0)
                return;
            if (!mBundles.TryGetValue(bundleName, out var current) || !ReferenceEquals(current, loadedBundle))
                return;

            mBundles.Remove(bundleName);
            loadedBundle.Handle?.Unload(mUnloadAllLoadedObjects);
            loadedBundle.Handle   = null;
            loadedBundle.LoadTask = null;
        }

        /// <summary>
        /// Validates that one bundle result exists and matches the requested type.
        /// </summary>
        private static void ValidateAsset(string path, Type assetType, object asset)
        {
            if (asset == null || asset is Object unityAsset && unityAsset == null)
                throw new InvalidOperationException($"AssetBundle asset '{path}' of type '{assetType.Name}' was not found.");
            if (!assetType.IsInstanceOfType(asset))
                throw new InvalidOperationException($"AssetBundle returned '{asset.GetType().Name}' for requested type '{assetType.Name}'.");
        }
    }

    /// <summary>
    /// Stores one loaded or in-flight bundle and its shared reference state.
    /// </summary>
    internal sealed class LoadedBundle
    {
        /// <summary>
        /// Stores the opened bundle handle.
        /// </summary>
        internal IAssetBundleHandle Handle;

        /// <summary>
        /// Stores one shared in-flight bundle request.
        /// </summary>
        internal Task<IAssetBundleHandle> LoadTask;

        /// <summary>
        /// Stores the number of assets currently retaining this bundle.
        /// </summary>
        internal int ReferenceCount;

        /// <summary>
        /// Stores the number of callers awaiting the shared bundle request.
        /// </summary>
        internal int PendingCount;
    }

    /// <summary>
    /// Stores the dependency-first bundle order retained by one asset acquisition.
    /// </summary>
    internal readonly struct AssetBundleLease
    {
        /// <summary>
        /// Stores the immutable precomputed bundle order.
        /// </summary>
        internal readonly string[] LoadOrder;

        /// <summary>
        /// Initializes one asset acquisition lease.
        /// </summary>
        internal AssetBundleLease(string[] loadOrder)
        {
            LoadOrder = loadOrder;
        }
    }

    /// <summary>
    /// Compares provider asset identities without invoking overloaded Unity equality.
    /// </summary>
    internal sealed class ReferenceComparer : IEqualityComparer<object>
    {
        /// <summary>
        /// Stores the shared stateless comparer instance.
        /// </summary>
        internal static readonly ReferenceComparer Instance = new();

        /// <summary>
        /// Prevents external comparer construction.
        /// </summary>
        private ReferenceComparer() { }

        /// <inheritdoc />
        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        /// <inheritdoc />
        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
