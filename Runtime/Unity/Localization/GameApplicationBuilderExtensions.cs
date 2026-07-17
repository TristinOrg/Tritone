using System;
using Tritone.Kernel;
using Tritone.Localization;
using Tritone.Unity.Tables;

namespace Tritone.Unity.Localization
{
    /// <summary>
    /// Provides table-backed localization setup for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds localization using tables beneath Localization/{language}.
        /// </summary>
        /// <param name="builder">The application builder receiving localization.</param>
        /// <param name="initialLanguage">The language loaded during startup.</param>
        /// <returns>The same application builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseLocalization(
            this GameApplicationBuilder builder,
            string initialLanguage)
        {
            return UseLocalization(builder,
                                   initialLanguage,
                                   language => $"Localization/{language}");
        }

        /// <summary>
        /// Adds localization with a replaceable language table path resolver.
        /// </summary>
        /// <param name="builder">The application builder receiving localization.</param>
        /// <param name="initialLanguage">The language loaded during startup.</param>
        /// <param name="pathResolver">The function resolving language table paths.</param>
        /// <returns>The same application builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseLocalization(
            this GameApplicationBuilder builder,
            string initialLanguage,
            Func<string, string> pathResolver)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            return builder.AddModule(new LocalizationModule(initialLanguage, pathResolver),
                                     typeof(TableModule));
        }
    }
}
