using System;
using Tritone.Kernel;

namespace Tritone.Diagnostics
{
    /// <summary>
    /// Creates category-bound module loggers and owns the application-wide logger.
    /// </summary>
    public sealed class ModuleLoggerFactory : IModuleLoggerFactory
    {
        /// <summary>
        /// Stores the application-wide logger owned by this factory.
        /// </summary>
        private Logger mLogger;

        /// <summary>
        /// Initializes a module logger factory.
        /// </summary>
        /// <param name="minimumLevel">The global minimum severity accepted by the logger.</param>
        /// <param name="sinks">The destinations that receive accepted events.</param>
        public ModuleLoggerFactory(ELogLevel minimumLevel, params ILogSink[] sinks)
        {
            mLogger = new(minimumLevel, sinks);
        }

        /// <summary>
        /// Creates a logger bound to one concrete module type.
        /// </summary>
        /// <param name="moduleType">The concrete module type used as the log category.</param>
        /// <param name="minimumLevel">The minimum severity accepted by the module.</param>
        /// <returns>A logger bound to the supplied module type.</returns>
        public IModuleLogger Create(Type moduleType, ELogLevel minimumLevel)
        {
            if (mLogger == null)
                throw new ObjectDisposedException(nameof(ModuleLoggerFactory));
            if (moduleType == null)
                throw new ArgumentNullException(nameof(moduleType));

            return new ModuleLogger(mLogger, moduleType.Name, minimumLevel);
        }

        /// <summary>
        /// Releases the application-wide logger and all sinks owned by it.
        /// </summary>
        public void Dispose()
        {
            if (mLogger == null)
                return;

            mLogger.Dispose();
            mLogger = null;
        }
    }
}
