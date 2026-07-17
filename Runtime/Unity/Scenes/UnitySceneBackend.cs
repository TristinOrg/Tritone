using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tritone.Unity.Scenes
{
    /// <summary>
    /// Executes additive Unity scene loading, activation, progress, and unloading.
    /// </summary>
    public sealed class UnitySceneBackend : ISceneBackend
    {
        /// <inheritdoc />
        public string ActiveSceneName => SceneManager.GetActiveScene().name;

        /// <inheritdoc />
        public bool IsLoaded(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        /// <inheritdoc />
        public async Task LoadAsync(string sceneName, Action<float> progress)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
                throw new InvalidOperationException($"Unity could not start loading scene '{sceneName}'.");

            while (!operation.isDone)
            {
                progress?.Invoke(Mathf.Clamp01(operation.progress / 0.9f));
                await Task.Yield();
            }
            progress?.Invoke(1.0f);
        }

        /// <inheritdoc />
        public void SetActive(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException($"Scene '{sceneName}' is not loaded.");
            if (!SceneManager.SetActiveScene(scene))
                throw new InvalidOperationException($"Unity could not activate scene '{sceneName}'.");
        }

        /// <inheritdoc />
        public async Task UnloadAsync(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            var operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null)
                throw new InvalidOperationException($"Unity could not start unloading scene '{sceneName}'.");
            while (!operation.isDone)
                await Task.Yield();
        }
    }
}
