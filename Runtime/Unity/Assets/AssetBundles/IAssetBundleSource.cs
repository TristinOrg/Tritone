using System.Threading.Tasks;

namespace Tritone.Unity.Assets.AssetBundles
{
    /// <summary>
    /// Opens bundle handles from one local, remote, or cached storage backend.
    /// </summary>
    public interface IAssetBundleSource
    {
        /// <summary>
        /// Opens one bundle synchronously.
        /// </summary>
        /// <param name="bundleName">The stable logical bundle name.</param>
        /// <param name="fileName">The source-specific bundle file name.</param>
        /// <returns>The opened bundle handle, or null when it cannot be opened.</returns>
        IAssetBundleHandle LoadBundle(string bundleName, string fileName);

        /// <summary>
        /// Opens one bundle asynchronously.
        /// </summary>
        /// <param name="bundleName">The stable logical bundle name.</param>
        /// <param name="fileName">The source-specific bundle file name.</param>
        /// <returns>A task containing the opened bundle handle, or null when it cannot be opened.</returns>
        Task<IAssetBundleHandle> LoadBundleAsync(string bundleName, string fileName);
    }
}
