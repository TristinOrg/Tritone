using System;
using System.Threading.Tasks;

namespace Tritone.Unity.Assets.AssetBundles
{
    /// <summary>
    /// Provides asset access to one loaded bundle without exposing its storage implementation.
    /// </summary>
    public interface IAssetBundleHandle
    {
        /// <summary>
        /// Loads one asset synchronously from this bundle.
        /// </summary>
        /// <param name="assetName">The provider-specific asset name inside the bundle.</param>
        /// <param name="assetType">The requested runtime asset type.</param>
        /// <returns>The loaded asset, or null when it cannot be found.</returns>
        object LoadAsset(string assetName, Type assetType);

        /// <summary>
        /// Loads one asset asynchronously from this bundle.
        /// </summary>
        /// <param name="assetName">The provider-specific asset name inside the bundle.</param>
        /// <param name="assetType">The requested runtime asset type.</param>
        /// <returns>A task containing the loaded asset, or null when it cannot be found.</returns>
        Task<object> LoadAssetAsync(string assetName, Type assetType);

        /// <summary>
        /// Unloads this bundle after its final provider reference is released.
        /// </summary>
        /// <param name="unloadAllLoadedObjects">Whether Unity should also unload objects created from this bundle.</param>
        void Unload(bool unloadAllLoadedObjects);
    }
}
