using System;

namespace Tritone.Kernel
{
    /// <summary>
    /// Stores a factory that creates a fresh scene module for each activation.
    /// </summary>
    internal sealed class SceneModuleRegistration
    {
        internal readonly Type          ModuleType;
        internal readonly Func<IModule> Factory;

        /// <summary>
        /// Initializes one scene module registration.
        /// </summary>
        /// <param name="moduleType">The concrete registered module type.</param>
        /// <param name="factory">The factory invoked for every activation.</param>
        internal SceneModuleRegistration(Type moduleType, Func<IModule> factory)
        {
            ModuleType = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
            Factory    = factory ?? throw new ArgumentNullException(nameof(factory));
        }
    }
}
