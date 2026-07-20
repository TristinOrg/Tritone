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

        /// <summary>
        /// Configures recent log capture and sampled runtime diagnostics.
        /// </summary>
        /// <param name="builder">The application builder receiving diagnostic infrastructure.</param>
        /// <param name="minimumLevel">The global minimum severity accepted by the logger.</param>
        /// <param name="logCapacity">The number of recent events retained in memory.</param>
        /// <param name="sampleWindow">The frame sampling window in seconds.</param>
        /// <param name="additionalSinks">Additional log destinations such as the Unity Console.</param>
        /// <returns>The supplied builder so additional configuration can be chained.</returns>
        public static GameApplicationBuilder UseRuntimeDiagnostics(this GameApplicationBuilder builder, ELogLevel minimumLevel = ELogLevel.Debug, int logCapacity = 128, double sampleWindow = 0.5, params ILogSink[] additionalSinks)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (additionalSinks == null)
                throw new ArgumentNullException(nameof(additionalSinks));

            var logs  = new RuntimeLogBufferSink(logCapacity);
            var sinks = new ILogSink[additionalSinks.Length + 1];
            sinks[0] = logs;
            for (var i = 0; i < additionalSinks.Length; i++)
                sinks[i + 1] = additionalSinks[i];

            builder.UseModuleLoggerFactory(new ModuleLoggerFactory(minimumLevel, sinks));
            return builder.AddModule(new RuntimeDiagnosticsModule(logs, sampleWindow));
        }
    }
}
