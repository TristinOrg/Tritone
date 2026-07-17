using System;
using Tritone.Content;
using Tritone.Kernel;
using Tritone.Unity.Assets.AssetBundles;

namespace Tritone.Unity.ContentUpdates
{
    /// <summary>
    /// Configures transactional content updates and manifest-managed AssetBundle loading together.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds HTTP content updates and local manifest-managed AssetBundle loading.
        /// </summary>
        /// <param name="builder">The application builder receiving content infrastructure.</param>
        /// <param name="options">The remote, local, retry, and timeout configuration.</param>
        /// <returns>The same application builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseContentAssets(
            this GameApplicationBuilder builder,
            ContentUpdateOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            UnityJsonContentManifestSerializer serializer = new();
            HttpContentUpdateSource updateSource           = new(options);
            FileAssetBundleSource bundleSource             = new(options.Settings.RootPath);
            return UseContentAssets(builder,
                                    options.Settings,
                                    updateSource,
                                    serializer,
                                    bundleSource);
        }

        /// <summary>
        /// Adds content updates and AssetBundle loading with replaceable transport contracts.
        /// </summary>
        /// <param name="builder">The application builder receiving content infrastructure.</param>
        /// <param name="settings">The validated local content storage settings.</param>
        /// <param name="updateSource">The remote content transport.</param>
        /// <param name="serializer">The manifest transport serializer.</param>
        /// <param name="bundleSource">The installed AssetBundle source.</param>
        /// <returns>The same application builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseContentAssets(
            this GameApplicationBuilder builder,
            ContentUpdateSettings settings,
            IContentUpdateSource updateSource,
            IContentManifestSerializer serializer,
            IAssetBundleSource bundleSource)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            ContentUpdater updater                      = new(settings, updateSource, serializer);
            ContentAssetBundleProvider provider         = new(settings, serializer, bundleSource);
            ContentUpdateModule updateModule            = new(updater,
                                                               provider.BeginUpdate,
                                                               provider.EndUpdate,
                                                               provider.Activate);
            builder.AddModule(updateModule);
            return Tritone.Unity.Assets.GameApplicationBuilderExtensions.UseAssets(builder,
                                                                                    provider);
        }
    }
}
