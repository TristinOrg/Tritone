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
        /// Adds asset management backed by Unity Addressables.
        /// </summary>
        /// <param name="builder">The application builder receiving the asset module.</param>
        /// <returns>The same builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseAddressableAssets(this GameApplicationBuilder builder)
        {
            return UseAddressableAssets(builder, new UnityAddressablesCatalogBackend());
        }

        /// <summary>
        /// Adds Addressables asset and remote catalog management with a replaceable catalog backend.
        /// </summary>
        /// <param name="builder">The application builder receiving the asset modules.</param>
        /// <param name="catalogBackend">The backend executing remote catalog operations.</param>
        /// <returns>The same builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseAddressableAssets(this GameApplicationBuilder builder, IAddressablesCatalogBackend catalogBackend)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (catalogBackend == null)
                throw new ArgumentNullException(nameof(catalogBackend));

            builder.AddModule(new AddressablesCatalogModule(catalogBackend));
            return UseAssets(builder, new AddressablesAssetProvider());
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
