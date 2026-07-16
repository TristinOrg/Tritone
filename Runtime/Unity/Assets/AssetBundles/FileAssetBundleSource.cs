using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Tritone.Unity.Assets.AssetBundles
{
    /// <summary>
    /// Opens standard Unity AssetBundles from one validated local root directory.
    /// </summary>
    public sealed class FileAssetBundleSource : IAssetBundleSource
    {
        /// <summary>
        /// Stores the normalized local bundle root with a trailing separator.
        /// </summary>
        private readonly string mRootPath;

        /// <summary>
        /// Initializes one local file bundle source.
        /// </summary>
        /// <param name="rootPath">The root directory containing bundle files.</param>
        public FileAssetBundleSource(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("An AssetBundle root path cannot be null, empty, or whitespace.", nameof(rootPath));

            mRootPath = Path.GetFullPath(rootPath)
                            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                        Path.DirectorySeparatorChar;
        }

        /// <inheritdoc />
        public IAssetBundleHandle LoadBundle(string bundleName, string fileName)
        {
            var path   = GetValidatedPath(fileName);
            var bundle = AssetBundle.LoadFromFile(path);
            return bundle == null ? null : new UnityAssetBundleHandle(bundle);
        }

        /// <inheritdoc />
        public Task<IAssetBundleHandle> LoadBundleAsync(string bundleName, string fileName)
        {
            var request = AssetBundle.LoadFromFileAsync(GetValidatedPath(fileName));
            if (request.isDone)
                return Task.FromResult<IAssetBundleHandle>(request.assetBundle == null
                    ? null
                    : new UnityAssetBundleHandle(request.assetBundle));

            TaskCompletionSource<IAssetBundleHandle> completion = new();
            request.completed += _ => completion.TrySetResult(request.assetBundle == null
                ? null
                : new UnityAssetBundleHandle(request.assetBundle));
            return completion.Task;
        }

        /// <summary>
        /// Resolves one file and prevents traversal outside the configured root.
        /// </summary>
        private string GetValidatedPath(string fileName)
        {
            var path = Path.GetFullPath(Path.Combine(mRootPath, fileName));
            if (!path.StartsWith(mRootPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"AssetBundle file '{fileName}' escapes the configured root directory.");
            return path;
        }
    }

    /// <summary>
    /// Adapts one standard Unity AssetBundle to the provider handle contract.
    /// </summary>
    internal sealed class UnityAssetBundleHandle : IAssetBundleHandle
    {
        /// <summary>
        /// Stores the wrapped Unity AssetBundle.
        /// </summary>
        private AssetBundle mBundle;

        /// <summary>
        /// Initializes one loaded Unity bundle handle.
        /// </summary>
        internal UnityAssetBundleHandle(AssetBundle bundle)
        {
            mBundle = bundle ?? throw new ArgumentNullException(nameof(bundle));
        }

        /// <inheritdoc />
        public object LoadAsset(string assetName, Type assetType)
        {
            ThrowIfUnloaded();
            return mBundle.LoadAsset(assetName, assetType);
        }

        /// <inheritdoc />
        public Task<object> LoadAssetAsync(string assetName, Type assetType)
        {
            ThrowIfUnloaded();
            var request = mBundle.LoadAssetAsync(assetName, assetType);
            if (request.isDone)
                return Task.FromResult((object)request.asset);

            TaskCompletionSource<object> completion = new();
            request.completed += _ => completion.TrySetResult(request.asset);
            return completion.Task;
        }

        /// <inheritdoc />
        public void Unload(bool unloadAllLoadedObjects)
        {
            if (mBundle == null)
                return;

            mBundle.Unload(unloadAllLoadedObjects);
            mBundle = null;
        }

        /// <summary>
        /// Rejects asset requests after this handle has been unloaded.
        /// </summary>
        private void ThrowIfUnloaded()
        {
            if (mBundle == null)
                throw new ObjectDisposedException(nameof(UnityAssetBundleHandle));
        }
    }
}
