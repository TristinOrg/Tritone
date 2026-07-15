using System;
using System.Collections.Generic;

namespace Tritone.Kernel
{
    /// <summary>
    /// Collects modules and builds an application with deterministic dependency order.
    /// </summary>
    public sealed class GameApplicationBuilder
    {
        /// <summary>
        /// Stores module registrations in their original declaration order.
        /// </summary>
        private readonly List<ModuleRegistration> mModules = new();

        /// <summary>
        /// Stores factories for scene modules that are created only when activated.
        /// </summary>
        private readonly List<SceneModuleRegistration> mSceneModules = new();

        /// <summary>
        /// Stores the application logger factory or the default no-op implementation.
        /// </summary>
        private IModuleLoggerFactory mLoggerFactory = NullModuleLoggerFactory.Instance;

        /// <summary>
        /// Stores the optional scene module activated immediately after persistent startup.
        /// </summary>
        private Type mInitialSceneModuleType;

        /// <summary>
        /// Indicates whether this builder has already transferred ownership to an application.
        /// </summary>
        private bool mBuilt;

        /// <summary>
        /// Configures the logger factory owned by the resulting application.
        /// </summary>
        /// <param name="loggerFactory">The factory used to create category-bound module loggers.</param>
        /// <returns>This builder so additional configuration can be chained.</returns>
        public GameApplicationBuilder UseModuleLoggerFactory(IModuleLoggerFactory loggerFactory)
        {
            ThrowIfBuilt();
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));
            if (!ReferenceEquals(mLoggerFactory, NullModuleLoggerFactory.Instance))
                throw new InvalidOperationException("A module logger factory is already configured.");

            mLoggerFactory = loggerFactory;
            return this;
        }

        /// <summary>
        /// Adds one module and declares the module types that must start before it.
        /// </summary>
        /// <typeparam name="TModule">The concrete type of the module.</typeparam>
        /// <param name="module">The module instance to add.</param>
        /// <param name="dependencies">The concrete module types that must start first.</param>
        /// <returns>This builder so additional modules can be chained.</returns>
        public GameApplicationBuilder AddModule<TModule>(TModule module, params Type[] dependencies)
            where TModule : class, IModule
        {
            ThrowIfBuilt();
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            mModules.Add(new ModuleRegistration(module, dependencies ?? Array.Empty<Type>()));
            return this;
        }

        /// <summary>
        /// Registers a scene module using its parameterless constructor.
        /// </summary>
        /// <typeparam name="TModule">The concrete scene module type.</typeparam>
        /// <returns>This builder so additional modules can be chained.</returns>
        public GameApplicationBuilder AddSceneModule<TModule>()
            where TModule : class, IModule, new()
        {
            return AddSceneModule(() => new TModule());
        }

        /// <summary>
        /// Registers a factory that creates a fresh scene module for each activation.
        /// </summary>
        /// <typeparam name="TModule">The concrete scene module type.</typeparam>
        /// <param name="factory">The factory invoked whenever this module is entered.</param>
        /// <returns>This builder so additional modules can be chained.</returns>
        public GameApplicationBuilder AddSceneModule<TModule>(Func<TModule> factory)
            where TModule : class, IModule
        {
            ThrowIfBuilt();
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var moduleType = typeof(TModule);
            for (int i = 0, cnt = mSceneModules.Count; i < cnt; i++)
            {
                if (mSceneModules[i].ModuleType == moduleType)
                    throw new InvalidOperationException($"Scene module '{moduleType.FullName}' is already registered.");
            }

            mSceneModules.Add(new SceneModuleRegistration(moduleType, () => factory()));
            return this;
        }

        /// <summary>
        /// Selects the registered scene module entered after persistent modules start.
        /// </summary>
        /// <typeparam name="TModule">The concrete registered scene module type.</typeparam>
        /// <returns>This builder so additional configuration can be chained.</returns>
        public GameApplicationBuilder UseInitialSceneModule<TModule>() where TModule : class, IModule
        {
            ThrowIfBuilt();
            if (mInitialSceneModuleType != null)
                throw new InvalidOperationException("An initial scene module is already configured.");

            mInitialSceneModuleType = typeof(TModule);
            return this;
        }

        /// <summary>
        /// Validates the module graph and creates an application instance.
        /// </summary>
        /// <returns>A new application in the created state.</returns>
        public GameApplication Build()
        {
            ThrowIfBuilt();
            var modules = ModuleGraph.Sort(mModules);
            for (int i = 0, cnt = mSceneModules.Count; i < cnt; i++)
            {
                for (int j = 0, moduleCount = modules.Length; j < moduleCount; j++)
                {
                    if (mSceneModules[i].ModuleType == modules[j].ModuleType)
                        throw new InvalidOperationException($"Module '{modules[j].ModuleType.FullName}' cannot be both persistent and scene-scoped.");
                }
            }
            if (mInitialSceneModuleType != null)
            {
                var found = false;
                for (int i = 0, cnt = mSceneModules.Count; i < cnt; i++)
                {
                    if (mSceneModules[i].ModuleType != mInitialSceneModuleType)
                        continue;
                    found = true;
                    break;
                }
                if (!found)
                    throw new InvalidOperationException($"Initial scene module '{mInitialSceneModuleType.FullName}' is not registered.");
            }
            mBuilt = true;
            return new GameApplication(modules,
                                       mSceneModules.ToArray(),
                                       mInitialSceneModuleType,
                                       mLoggerFactory);
        }

        /// <summary>
        /// Rejects changes after this builder has created an application.
        /// </summary>
        private void ThrowIfBuilt()
        {
            if (mBuilt)
                throw new InvalidOperationException("This application builder has already been built.");
        }
    }
}
