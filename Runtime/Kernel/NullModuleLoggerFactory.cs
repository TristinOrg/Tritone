using System;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides the shared no-op logger when diagnostics are not configured.
    /// </summary>
    internal sealed class NullModuleLoggerFactory : IModuleLoggerFactory
    {
        /// <summary>
        /// Stores the shared no-op factory instance.
        /// </summary>
        internal static readonly NullModuleLoggerFactory Instance = new NullModuleLoggerFactory();

        /// <summary>
        /// Prevents external construction of the shared no-op factory.
        /// </summary>
        private NullModuleLoggerFactory() { }

        /// <summary>
        /// Gets the shared no-op module logger.
        /// </summary>
        /// <param name="moduleType">The ignored concrete module type.</param>
        /// <param name="minimumLevel">The ignored minimum severity.</param>
        /// <returns>The shared no-op module logger.</returns>
        public IModuleLogger Create(Type moduleType, ELogLevel minimumLevel)
        {
            return NullModuleLogger.Instance;
        }

        /// <summary>
        /// Performs no disposal work for the shared no-op factory.
        /// </summary>
        public void Dispose() { }
    }
}
