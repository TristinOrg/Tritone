using System;

namespace Tritone.Kernel
{
    /// <summary>
    /// Stores one module instance together with its declared dependencies.
    /// </summary>
    internal sealed class ModuleRegistration
    {
        /// <summary>
        /// Initializes module registration metadata.
        /// </summary>
        /// <param name="module">The module instance owned by the application.</param>
        /// <param name="dependencies">The concrete module types that must start first.</param>
        internal ModuleRegistration(IModule module, Type[] dependencies)
        {
            Module       = module;
            ModuleType   = module.GetType();
            Dependencies = dependencies;
        }

        /// <summary>
        /// Gets the module instance owned by the application.
        /// </summary>
        internal IModule Module { get; }

        /// <summary>
        /// Gets the concrete runtime type of the module.
        /// </summary>
        internal Type ModuleType { get; }

        /// <summary>
        /// Gets the concrete module types that must start first.
        /// </summary>
        internal Type[] Dependencies { get; }
    }
}
