using System;
using System.Threading.Tasks;
using Tritone.Assets;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Loads addressed Unity assets while preserving Tritone ownership semantics.
    /// </summary>
    public sealed class AddressablesAssetProvider : IAssetProvider
    {
        /// <inheritdoc />
        public object Load(string path, Type assetType)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new PlatformNotSupportedException("Synchronous Addressables loading is not available on WebGL. Use LoadAsync instead.");
#else
            ValidateAssetType(assetType);
            var handle = Addressables.LoadAssetAsync<Object>(path);
            try
            {
                var asset = handle.WaitForCompletion();
                ValidateResult(path, assetType, asset, handle.Status);
                return asset;
            }
            catch
            {
                Addressables.Release(handle);
                throw;
            }
#endif
        }

        /// <inheritdoc />
        public async Task<object> LoadAsync(string path, Type assetType)
        {
            ValidateAssetType(assetType);
            var handle = Addressables.LoadAssetAsync<Object>(path);
            try
            {
                var asset = await handle.Task;
                ValidateResult(path, assetType, asset, handle.Status);
                return asset;
            }
            catch
            {
                Addressables.Release(handle);
                throw;
            }
        }

        /// <inheritdoc />
        public void Release(object asset)
        {
            if (asset is Object unityAsset)
                Addressables.Release(unityAsset);
        }

        /// <summary>
        /// Ensures Addressables receives a supported Unity object type.
        /// </summary>
        /// <param name="assetType">The requested runtime asset type.</param>
        private static void ValidateAssetType(Type assetType)
        {
            if (assetType == null)
                throw new ArgumentNullException(nameof(assetType));
            if (!typeof(Object).IsAssignableFrom(assetType))
                throw new ArgumentException($"Addressables can only load UnityEngine.Object types, not '{assetType.Name}'.", nameof(assetType));
        }

        /// <summary>
        /// Ensures a completed Addressables operation returned the requested asset type.
        /// </summary>
        /// <param name="path">The requested address.</param>
        /// <param name="assetType">The requested runtime asset type.</param>
        /// <param name="asset">The completed operation result.</param>
        /// <param name="status">The completed operation status.</param>
        private static void ValidateResult(string path, Type assetType, Object asset, AsyncOperationStatus status)
        {
            if (status != AsyncOperationStatus.Succeeded || !asset)
                throw new InvalidOperationException($"Addressable asset '{path}' of type '{assetType.Name}' was not found.");
            if (!assetType.IsInstanceOfType(asset))
                throw new InvalidOperationException($"Addressables returned '{asset.GetType().Name}' for requested type '{assetType.Name}'.");
        }
    }
}
