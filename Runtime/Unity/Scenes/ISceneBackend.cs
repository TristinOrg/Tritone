using System;
using System.Threading.Tasks;

namespace Tritone.Unity.Scenes
{
    /// <summary>
    /// Abstracts Unity scene operations for deterministic framework testing.
    /// </summary>
    public interface ISceneBackend
    {
        /// <summary>
        /// Gets the name of Unity's currently active scene.
        /// </summary>
        string ActiveSceneName { get; }

        /// <summary>
        /// Determines whether one Unity scene is loaded.
        /// </summary>
        /// <param name="sceneName">The Unity scene name or path.</param>
        /// <returns>True when the scene is loaded; otherwise, false.</returns>
        bool IsLoaded(string sceneName);

        /// <summary>
        /// Loads one scene additively without replacing the active scene.
        /// </summary>
        /// <param name="sceneName">The Unity scene name or path.</param>
        /// <param name="progress">The optional normalized loading progress callback.</param>
        /// <returns>A task completed after the scene is fully loaded.</returns>
        Task LoadAsync(string sceneName, Action<float> progress);

        /// <summary>
        /// Makes one loaded scene active.
        /// </summary>
        /// <param name="sceneName">The loaded Unity scene name or path.</param>
        void SetActive(string sceneName);

        /// <summary>
        /// Unloads one previously loaded scene.
        /// </summary>
        /// <param name="sceneName">The loaded Unity scene name or path.</param>
        /// <returns>A task completed after the scene is unloaded.</returns>
        Task UnloadAsync(string sceneName);
    }
}
