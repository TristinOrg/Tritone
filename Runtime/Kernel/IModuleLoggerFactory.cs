using System;

namespace Tritone.Kernel
{
    /// <summary>
    /// Defines creation and ownership of category-bound module loggers.
    /// </summary>
    public interface IModuleLoggerFactory : IDisposable
    {
        /// <summary>
        /// Creates a logger bound to one concrete module type.
        /// </summary>
        /// <param name="moduleType">The concrete module type used as the log category.</param>
        /// <param name="minimumLevel">The minimum severity accepted by the module.</param>
        /// <returns>A logger bound to the supplied module type.</returns>
        IModuleLogger Create(Type moduleType, ELogLevel minimumLevel);
    }
}
