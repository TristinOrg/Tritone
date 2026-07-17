using System;
using System.Threading.Tasks;
using Tritone.Kernel;

namespace Tritone.Scenes
{
    /// <summary>
    /// Coordinates Unity scene loading with Tritone scene module activation.
    /// </summary>
    public interface ISceneService
    {
        /// <summary>
        /// Gets the currently active managed Unity scene name.
        /// </summary>
        string ActiveSceneName { get; }

        /// <summary>
        /// Gets whether one scene transition is currently running.
        /// </summary>
        bool IsSwitching { get; }

        /// <summary>
        /// Loads a Unity scene before activating its registered scene module.
        /// </summary>
        /// <typeparam name="TModule">The registered scene module type.</typeparam>
        /// <param name="sceneName">The Unity scene name or path.</param>
        /// <param name="progress">The optional normalized loading progress callback.</param>
        /// <returns>A task containing the active scene module.</returns>
        Task<TModule> SwitchAsync<TModule>(string sceneName, Action<float> progress = null)
            where TModule : class, IModule;
    }
}
