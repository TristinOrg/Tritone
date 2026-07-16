using System;
using System.Threading.Tasks;

namespace Tritone.Assets
{
    /// <summary>
    /// Loads assets from one concrete storage system without exposing it to game modules.
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>
        /// Loads one asset synchronously.
        /// </summary>
        /// <param name="path">The provider-specific asset path.</param>
        /// <param name="assetType">The requested runtime asset type.</param>
        /// <returns>The loaded asset, or null when the path cannot be resolved.</returns>
        object Load(string path, Type assetType);

        /// <summary>
        /// Loads one asset asynchronously.
        /// </summary>
        /// <param name="path">The provider-specific asset path.</param>
        /// <param name="assetType">The requested runtime asset type.</param>
        /// <returns>A task containing the loaded asset, or null when the path cannot be resolved.</returns>
        Task<object> LoadAsync(string path, Type assetType);

        /// <summary>
        /// Releases the provider ownership associated with one loaded asset.
        /// </summary>
        /// <param name="asset">The asset whose framework reference count reached zero.</param>
        void Release(object asset);
    }
}
