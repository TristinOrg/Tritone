using System;
using Tritone.Kernel;
using Tritone.Saves;
using Tritone.Settings;

namespace Tritone.Unity.Settings
{
    /// <summary>
    /// Provides persistent typed settings setup for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds settings persisted through the configured save service.
        /// </summary>
        /// <param name="builder">The application builder receiving settings infrastructure.</param>
        /// <param name="slot">The durable save slot name.</param>
        /// <returns>The same application builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseSettings(this GameApplicationBuilder builder,
                                                         string slot = "settings")
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            return builder.AddModule(new SettingsModule(slot), typeof(SaveModule));
        }
    }
}
