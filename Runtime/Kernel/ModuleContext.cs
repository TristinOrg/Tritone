using System;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides immutable application infrastructure during module configuration.
    /// </summary>
    public sealed class ModuleContext
    {
        /// <summary>
        /// Initializes the context shared by all modules in one application.
        /// </summary>
        /// <param name="services">The application-scoped service registry.</param>
        /// <param name="loggerFactory">The factory that creates category-bound module loggers.</param>
        internal ModuleContext(IServiceRegistry services, IModuleLoggerFactory loggerFactory)
        {
            Services      = services ?? throw new ArgumentNullException(nameof(services));
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Gets the application-scoped service registry.
        /// </summary>
        public IServiceRegistry Services { get; }

        /// <summary>
        /// Gets the factory that creates category-bound module loggers.
        /// </summary>
        public IModuleLoggerFactory LoggerFactory { get; }
    }
}
