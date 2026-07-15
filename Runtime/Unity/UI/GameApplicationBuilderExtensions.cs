using System;
using Tritone.Kernel;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Provides UI infrastructure configuration for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds catalogued window management to the application.
        /// </summary>
        /// <param name="builder">The application builder receiving UI infrastructure.</param>
        /// <param name="root">The scene UI layer root.</param>
        /// <param name="catalog">The project window catalog.</param>
        /// <returns>The supplied builder for chained configuration.</returns>
        public static GameApplicationBuilder UseUI(this GameApplicationBuilder builder,
                                                   UIRoot root,
                                                   UIWindowCatalog catalog)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.AddModule(new UIModule(root, catalog));
        }
    }
}
