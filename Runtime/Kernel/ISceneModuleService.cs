using System;

namespace Tritone.Kernel
{
    /// <summary>
    /// Controls the single dynamic scene module active inside an application.
    /// </summary>
    public interface ISceneModuleService
    {
        /// <summary>Gets the concrete type of the active scene module.</summary>
        Type ActiveModuleType { get; }

        /// <summary>Stops the current scene module and starts a new registered type.</summary>
        /// <param name="moduleType">The registered scene module type to activate.</param>
        /// <returns>The newly created module instance.</returns>
        object SwitchModule(Type moduleType);

        /// <summary>Stops and removes the active scene module.</summary>
        void ExitModule();
    }
}
