using System;
using System.Threading.Tasks;
using Tritone.Scenes;

namespace Tritone.Kernel
{

    /// <summary>
    /// Provides scene and scene-module switching operations.
    /// </summary>
    public sealed class SceneCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        /// <summary>
        /// Initializes scene operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal SceneCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Stops the current scene module and enters one registered module.
        /// </summary>
        /// <typeparam name="TModule">The registered scene module type.</typeparam>
        /// <returns>The newly active scene module.</returns>
        public TModule SwitchModule<TModule>() where TModule : class, IModule
        {
            var service = mContext.GetRequired<ISceneModuleService>(
                "Scene module infrastructure is unavailable.");
            return (TModule)service.SwitchModule(typeof(TModule));
        }

        /// <summary>
        /// Loads a Unity scene and switches its registered scene module.
        /// </summary>
        /// <typeparam name="TModule">The registered scene module type.</typeparam>
        /// <param name="sceneName">The Unity scene name or path.</param>
        /// <param name="progress">The optional normalized progress callback.</param>
        /// <returns>A task containing the active scene module.</returns>
        public Task<TModule> SwitchAsync<TModule>(string sceneName,
                                                  Action<float> progress)
            where TModule : class, IModule
        {
            var service = mContext.GetRequired<ISceneService>(
                "Scene infrastructure is not configured. Call builder.UseScenes() before adding game modules.");
            return service.SwitchAsync<TModule>(sceneName, progress);
        }
    }
}
