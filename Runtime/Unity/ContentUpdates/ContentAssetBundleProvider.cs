using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Tritone.Assets;
using Tritone.Content;
using Tritone.Unity.Assets.AssetBundles;

namespace Tritone.Unity.ContentUpdates
{
    /// <summary>
    /// Activates manifest-backed AssetBundle providers only when no old content references remain.
    /// </summary>
    public sealed class ContentAssetBundleProvider : IAssetProvider
    {
        // Stores UTF-8 encoding without a byte-order mark.
        private static readonly UTF8Encoding sEncoding = new(false);

        // Stores validated local content paths.
        private readonly ContentUpdateSettings mSettings;

        // Converts the installed manifest from durable local storage.
        private readonly IContentManifestSerializer mSerializer;

        // Opens physical AssetBundle handles beneath the local content root.
        private readonly IAssetBundleSource mBundleSource;

        // Stores provider ownership for every returned asset object.
        private readonly Dictionary<object, List<AssetBundleAssetProvider>> mAssetLeases =
            new(ReferenceComparer.Instance);

        // Stores the currently active immutable manifest provider.
        private AssetBundleAssetProvider mProvider;

        // Stores the number of active provider asset references.
        private int mReferenceCount;

        // Stores the number of provider operations awaiting completion.
        private int mPendingCount;

        // Indicates whether content update commit and activation currently own provider access.
        private bool mUpdating;

        /// <summary>
        /// Initializes one content-managed AssetBundle provider and activates installed content when available.
        /// </summary>
        /// <param name="settings">The validated local content storage settings.</param>
        /// <param name="serializer">The local manifest serializer.</param>
        /// <param name="bundleSource">The source used to open installed AssetBundle files.</param>
        public ContentAssetBundleProvider(ContentUpdateSettings settings,
                                          IContentManifestSerializer serializer,
                                          IAssetBundleSource bundleSource)
        {
            mSettings     = settings ?? throw new ArgumentNullException(nameof(settings));
            mSerializer   = serializer ?? throw new ArgumentNullException(nameof(serializer));
            mBundleSource = bundleSource ?? throw new ArgumentNullException(nameof(bundleSource));

            if (File.Exists(mSettings.ManifestPath))
            {
                var content  = File.ReadAllText(mSettings.ManifestPath, sEncoding);
                var manifest = mSerializer.Deserialize(content);
                Activate(manifest);
            }
        }

        /// <inheritdoc />
        public object Load(string path, Type assetType)
        {
            var provider = GetRequiredProvider();
            var asset    = provider.Load(path, assetType);
            Track(asset, provider);
            return asset;
        }

        /// <inheritdoc />
        public async Task<object> LoadAsync(string path, Type assetType)
        {
            var provider = GetRequiredProvider();
            mPendingCount++;
            try
            {
                var asset = await provider.LoadAsync(path, assetType);
                Track(asset, provider);
                return asset;
            }
            finally
            {
                mPendingCount--;
            }
        }

        /// <inheritdoc />
        public void Release(object asset)
        {
            if (asset == null ||
                !mAssetLeases.TryGetValue(asset, out var leases) ||
                leases.Count == 0)
                return;

            var lastIndex = leases.Count - 1;
            var provider  = leases[lastIndex];
            leases.RemoveAt(lastIndex);
            if (leases.Count == 0)
                mAssetLeases.Remove(asset);

            mReferenceCount--;
            provider.Release(asset);
        }

        /// <summary>
        /// Activates one verified local manifest after all old content references are gone.
        /// </summary>
        /// <param name="manifest">The verified manifest to activate.</param>
        internal void Activate(ContentManifest manifest)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            if (mReferenceCount > 0 || mPendingCount > 0)
                throw new InvalidOperationException(
                    "Updated content cannot be activated while old assets are loaded or loading.");

            mProvider = new AssetBundleAssetProvider(manifest.CreateAssetBundleRegistry(),
                                                     mBundleSource);
        }

        /// <summary>
        /// Prevents new asset requests after confirming that old content has no active references.
        /// </summary>
        internal void BeginUpdate()
        {
            if (mUpdating)
                throw new InvalidOperationException("A content activation gate is already active.");
            if (mReferenceCount > 0 || mPendingCount > 0)
                throw new InvalidOperationException(
                    "Content cannot be updated while old assets are loaded or loading.");

            mUpdating = true;
        }

        /// <summary>
        /// Allows asset requests after content update completion or failure.
        /// </summary>
        internal void EndUpdate()
        {
            mUpdating = false;
        }

        /// <summary>
        /// Records which immutable provider must receive one future asset release.
        /// </summary>
        /// <param name="asset">The asset returned by the provider.</param>
        /// <param name="provider">The provider that owns the asset reference.</param>
        private void Track(object asset, AssetBundleAssetProvider provider)
        {
            if (!mAssetLeases.TryGetValue(asset, out var leases))
            {
                leases = new();
                mAssetLeases.Add(asset, leases);
            }
            leases.Add(provider);
            mReferenceCount++;
        }

        /// <summary>
        /// Gets the active provider or reports that initial content has not been installed.
        /// </summary>
        /// <returns>The currently active AssetBundle provider.</returns>
        private AssetBundleAssetProvider GetRequiredProvider()
        {
            if (mUpdating)
                throw new InvalidOperationException(
                    "Content assets cannot be loaded while an update is active.");

            return mProvider ??
                   throw new InvalidOperationException(
                       "No active content manifest is available. Complete UpdateContentAsync before loading content assets.");
        }
    }
}
