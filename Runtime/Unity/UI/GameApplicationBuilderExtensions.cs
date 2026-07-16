using System;
using Tritone.Kernel;
using Tritone.Unity.Assets;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Provides UI infrastructure configuration for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds hot-update-friendly window management without a central catalog.
        /// </summary>
        /// <param name="builder">The application builder receiving UI infrastructure.</param>
        /// <param name="root">The scene UI layer root.</param>
        /// <returns>The supplied builder for chained configuration.</returns>
        public static GameApplicationBuilder UseUI(this GameApplicationBuilder builder,
                                                   UIRoot root)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.AddModule(new UIModule(root), typeof(AssetModule));
        }

    }
}
