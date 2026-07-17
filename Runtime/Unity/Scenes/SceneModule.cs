using System;
using System.Threading.Tasks;
using Tritone.Kernel;
using Tritone.Scenes;

namespace Tritone.Unity.Scenes
{
    /// <summary>
    /// Coordinates additive Unity scene transitions with dynamic scene module lifetimes.
    /// </summary>
    public sealed class SceneModule : ModuleBase, ISceneService
    {
        // Executes concrete Unity scene operations.
        private readonly ISceneBackend mBackend;

        // Controls the single active Tritone scene module.
        private ISceneModuleService mModuleService;

        // Stores one shared in-flight scene transition.
        private Task<object> mSwitchTask;

        // Stores the module type targeted by the in-flight transition.
        private Type mPendingModuleType;

        // Stores the scene targeted by the in-flight transition.
        private string mPendingSceneName;

        // Indicates whether this service has permanently stopped.
        private bool mStopped;

        /// <inheritdoc />
        public string ActiveSceneName { get; private set; }

        /// <inheritdoc />
        public bool IsSwitching => mSwitchTask != null && !mSwitchTask.IsCompleted;

        /// <summary>
        /// Initializes scene management with Unity's default scene backend.
        /// </summary>
        public SceneModule() : this(new UnitySceneBackend()) { }

        /// <summary>
        /// Initializes scene management with one replaceable scene backend.
        /// </summary>
        /// <param name="backend">The backend executing Unity scene operations.</param>
        public SceneModule(ISceneBackend backend)
        {
            mBackend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        /// <summary>
        /// Registers application-wide scene transition access.
        /// </summary>
        /// <param name="services">The application service registry.</param>
        protected override void OnConfigure(IServiceRegistry services)
        {
            mModuleService  = services.GetRequired<ISceneModuleService>();
            ActiveSceneName = mBackend.ActiveSceneName;
            services.AddSingleton<ISceneService>(this);
        }

        /// <inheritdoc />
        public async Task<TModule> SwitchAsync<TModule>(string sceneName,
                                                        Action<float> progress = null)
            where TModule : class, IModule
        {
            return (TModule)await SwitchAsync(typeof(TModule), sceneName, progress);
        }

        /// <summary>
        /// Prevents new transitions after application shutdown begins.
        /// </summary>
        protected override void OnStop()
        {
            mStopped           = true;
            mModuleService     = null;
            mPendingModuleType = null;
            mPendingSceneName  = null;
        }

        /// <summary>
        /// Starts, reuses, or rejects one scene transition.
        /// </summary>
        /// <param name="moduleType">The registered scene module type.</param>
        /// <param name="sceneName">The Unity scene name or path.</param>
        /// <param name="progress">The optional normalized loading progress callback.</param>
        /// <returns>A task containing the active scene module.</returns>
        private Task<object> SwitchAsync(Type moduleType,
                                         string sceneName,
                                         Action<float> progress)
        {
            ValidateRequest(moduleType, sceneName);
            if (mSwitchTask != null && mSwitchTask.IsCompleted)
                ClearCompletedRequest();
            if (mSwitchTask != null)
            {
                if (mPendingModuleType == moduleType &&
                    string.Equals(mPendingSceneName, sceneName, StringComparison.Ordinal))
                    return mSwitchTask;

                throw new InvalidOperationException(
                    $"Cannot switch to scene '{sceneName}' while scene '{mPendingSceneName}' is loading.");
            }

            mPendingModuleType = moduleType;
            mPendingSceneName  = sceneName;
            mSwitchTask        = RunSwitchAsync(moduleType, sceneName, progress);
            return mSwitchTask;
        }

        /// <summary>
        /// Loads the target, activates its module, and then releases the previous scene.
        /// </summary>
        /// <param name="moduleType">The registered scene module type.</param>
        /// <param name="sceneName">The Unity scene name or path.</param>
        /// <param name="progress">The optional normalized loading progress callback.</param>
        /// <returns>A task containing the active scene module.</returns>
        private async Task<object> RunSwitchAsync(Type moduleType,
                                                  string sceneName,
                                                  Action<float> progress)
        {
            var previousScene      = ActiveSceneName;
            var previousModuleType = mModuleService.ActiveModuleType;
            var loadedTarget       = false;

            try
            {
                if (!string.Equals(previousScene, sceneName, StringComparison.Ordinal))
                {
                    if (!mBackend.IsLoaded(sceneName))
                    {
                        loadedTarget = true;
                        await mBackend.LoadAsync(sceneName, progress);
                    }
                    else
                        progress?.Invoke(1.0f);
                }
                else
                    progress?.Invoke(1.0f);

                if (mStopped)
                    throw new ObjectDisposedException(nameof(SceneModule));

                mBackend.SetActive(sceneName);
                object module;
                try
                {
                    module = mModuleService.SwitchModule(moduleType);
                }
                catch
                {
                    await RestorePreviousSceneAsync(previousScene,
                                                    previousModuleType,
                                                    sceneName,
                                                    loadedTarget);
                    throw;
                }

                ActiveSceneName = sceneName;
                if (!string.IsNullOrEmpty(previousScene) &&
                    !string.Equals(previousScene, sceneName, StringComparison.Ordinal) &&
                    mBackend.IsLoaded(previousScene))
                    await mBackend.UnloadAsync(previousScene);

                return module;
            }
            catch
            {
                if (loadedTarget &&
                    !string.Equals(ActiveSceneName, sceneName, StringComparison.Ordinal) &&
                    mBackend.IsLoaded(sceneName))
                    await mBackend.UnloadAsync(sceneName);
                throw;
            }
        }

        /// <summary>
        /// Restores the previous active scene and module after target module startup fails.
        /// </summary>
        /// <param name="previousScene">The previous active scene name.</param>
        /// <param name="previousModuleType">The previous scene module type.</param>
        /// <param name="targetScene">The newly loaded target scene.</param>
        /// <param name="loadedTarget">Whether this transition loaded the target scene.</param>
        /// <returns>A task completed after rollback cleanup.</returns>
        private async Task RestorePreviousSceneAsync(string previousScene,
                                                     Type previousModuleType,
                                                     string targetScene,
                                                     bool loadedTarget)
        {
            if (!string.IsNullOrEmpty(previousScene) && mBackend.IsLoaded(previousScene))
                mBackend.SetActive(previousScene);
            if (previousModuleType != null &&
                mModuleService.ActiveModuleType != previousModuleType)
                mModuleService.SwitchModule(previousModuleType);
            if (loadedTarget && mBackend.IsLoaded(targetScene))
                await mBackend.UnloadAsync(targetScene);
        }

        /// <summary>
        /// Clears metadata retained by a completed request.
        /// </summary>
        private void ClearCompletedRequest()
        {
            mSwitchTask        = null;
            mPendingModuleType = null;
            mPendingSceneName  = null;
        }

        /// <summary>
        /// Validates one requested module and Unity scene name.
        /// </summary>
        /// <param name="moduleType">The registered scene module type.</param>
        /// <param name="sceneName">The Unity scene name or path.</param>
        private void ValidateRequest(Type moduleType, string sceneName)
        {
            if (mStopped)
                throw new ObjectDisposedException(nameof(SceneModule));
            if (moduleType == null)
                throw new ArgumentNullException(nameof(moduleType));
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new ArgumentException(
                    "A scene name cannot be null, empty, or whitespace.",
                    nameof(sceneName));
        }
    }
}
