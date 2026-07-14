using System;
using Tritone.Kernel;

namespace Tritone.Diagnostics
{
    /// <summary>
    /// Provides diagnostic infrastructure configuration for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the application-wide logger used by every ModuleBase instance.
        /// </summary>
        /// <param name="builder">The application builder receiving the logging infrastructure.</param>
        /// <param name="minimumLevel">The global minimum severity accepted by the logger.</param>
        /// <param name="sinks">The destinations that receive accepted events.</param>
        /// <returns>The supplied builder so additional configuration can be chained.</returns>
        public static GameApplicationBuilder UseLogging(this GameApplicationBuilder builder,
                                                        ELogLevel minimumLevel,
                                                        params ILogSink[] sinks)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.UseModuleLoggerFactory(new ModuleLoggerFactory(minimumLevel, sinks));
        }
    }
}
