using System;
using Tritone.Assets;
using Tritone.Kernel;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Provides shared asset configuration for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds asset management backed by Unity Resources.
        /// </summary>
        public static GameApplicationBuilder UseAssets(this GameApplicationBuilder builder)
        {
            return UseAssets(builder, new ResourcesAssetProvider());
        }

        /// <summary>
        /// Adds asset management backed by a custom provider.
        /// </summary>
        public static GameApplicationBuilder UseAssets(this GameApplicationBuilder builder,
                                                        IAssetProvider provider)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return builder.AddModule(new AssetModule(provider));
        }
    }
}
