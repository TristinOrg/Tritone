using System;
using System.Collections.Generic;
using Tritone.Models;
using Tritone.Flows;
using Tritone.Entities;

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
        /// Stores explicit model factories and their ownership lifetimes.
        /// </summary>
        private readonly List<ModelRegistration> mModels = new();

        /// <summary>
        /// Stores explicit factories for application flows.
        /// </summary>
        private readonly List<FlowRegistration> mFlows = new();

        /// <summary>
        /// Stores explicit component registrations for entity worlds.
        /// </summary>
        private readonly List<ComponentRegistration> mEntityComponents = new();

        /// <summary>
        /// Stores explicit system registrations for entity worlds.
        /// </summary>
        private readonly List<EntitySystemRegistration> mEntitySystems = new();

        /// <summary>
        /// Stores the reserved capacity for each fresh entity world.
        /// </summary>
        private int mEntityInitialCapacity = 64;

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
        /// Registers an application model using its parameterless constructor.
        /// </summary>
        /// <typeparam name="TModel">The concrete model type.</typeparam>
        /// <returns>This builder so additional configuration can be chained.</returns>
        public GameApplicationBuilder AddApplicationModel<TModel>()
            where TModel : class, IModel, new()
        {
            return AddApplicationModel(() => new TModel());
        }

        /// <summary>
        /// Registers an application flow using its parameterless constructor.
        /// </summary>
        /// <typeparam name="TFlow">The concrete flow type.</typeparam>
        /// <returns>This builder so additional configuration can be chained.</returns>
        public GameApplicationBuilder AddFlow<TFlow>()
            where TFlow : class, IFlow, new()
        {
            return AddFlow(() => new TFlow());
        }

        /// <summary>
        /// Configures the reserved capacity used by fresh entity worlds.
        /// </summary>
        /// <param name="initialCapacity">The positive initial entity capacity.</param>
        /// <returns>This builder so additional configuration can be chained.</returns>
        public GameApplicationBuilder UseEntities(int initialCapacity = 64)
        {
            ThrowIfBuilt();
            if (initialCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            mEntityInitialCapacity = initialCapacity;
            return this;
        }

        /// <summary>
        /// Registers one application-world component type.
        /// </summary>
        public GameApplicationBuilder AddApplicationComponent<T>()
            where T : struct, IEntityComponent
        {
            return AddComponent<T>(EEntityWorldLifetime.Application);
        }

        /// <summary>
        /// Registers one scene-world component type.
        /// </summary>
        public GameApplicationBuilder AddSceneComponent<T>()
            where T : struct, IEntityComponent
        {
            return AddComponent<T>(EEntityWorldLifetime.Scene);
        }

        /// <summary>
        /// Registers an application-world entity system using its parameterless constructor.
        /// </summary>
        public GameApplicationBuilder AddApplicationEntitySystem<TSystem>()
            where TSystem : class, IEntitySystem, new()
        {
            return AddApplicationEntitySystem(() => new TSystem());
        }

        /// <summary>
        /// Registers an application-world entity system factory.
        /// </summary>
        /// <param name="factory">The factory creating one system per application world.</param>
        public GameApplicationBuilder AddApplicationEntitySystem<TSystem>(
            Func<TSystem> factory)
            where TSystem : class, IEntitySystem
        {
            return AddEntitySystem(factory, EEntityWorldLifetime.Application);
        }

        /// <summary>
        /// Registers a scene-world entity system using its parameterless constructor.
        /// </summary>
        public GameApplicationBuilder AddSceneEntitySystem<TSystem>()
            where TSystem : class, IEntitySystem, new()
        {
            return AddSceneEntitySystem(() => new TSystem());
        }

        /// <summary>
        /// Registers a scene-world entity system factory.
        /// </summary>
        /// <param name="factory">The factory creating one system per scene world.</param>
        public GameApplicationBuilder AddSceneEntitySystem<TSystem>(
            Func<TSystem> factory)
            where TSystem : class, IEntitySystem
        {
            return AddEntitySystem(factory, EEntityWorldLifetime.Scene);
        }

        /// <summary>
        /// Registers a factory that creates a fresh flow for each successful transition.
        /// </summary>
        /// <typeparam name="TFlow">The concrete flow type.</typeparam>
        /// <param name="factory">The explicit flow factory.</param>
        /// <returns>This builder so additional configuration can be chained.</returns>
        public GameApplicationBuilder AddFlow<TFlow>(Func<TFlow> factory)
            where TFlow : class, IFlow
        {
            ThrowIfBuilt();
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var flowType = typeof(TFlow);
            for (int i = 0, cnt = mFlows.Count; i < cnt; i++)
            {
                if (mFlows[i].FlowType == flowType)
                    throw new InvalidOperationException(
                        $"Flow '{flowType.FullName}' is already registered.");
            }

            mFlows.Add(new FlowRegistration(flowType, factory));
            return this;
        }

        /// <summary>
        /// Registers an application model factory.
        /// </summary>
        /// <typeparam name="TModel">The concrete model type.</typeparam>
        /// <param name="factory">The factory invoked when the model is first requested.</param>
        /// <returns>This builder so additional configuration can be chained.</returns>
        public GameApplicationBuilder AddApplicationModel<TModel>(Func<TModel> factory)
            where TModel : class, IModel
        {
            return AddModel(factory, EModelLifetime.Application);
        }

        /// <summary>
        /// Registers a scene model using its parameterless constructor.
        /// </summary>
        /// <typeparam name="TModel">The concrete model type.</typeparam>
        /// <returns>This builder so additional configuration can be chained.</returns>
        public GameApplicationBuilder AddSceneModel<TModel>()
            where TModel : class, IModel, new()
        {
            return AddSceneModel(() => new TModel());
        }

        /// <summary>
        /// Registers a scene model factory.
        /// </summary>
        /// <typeparam name="TModel">The concrete model type.</typeparam>
        /// <param name="factory">The factory invoked once per scene lifetime on first access.</param>
        /// <returns>This builder so additional configuration can be chained.</returns>
        public GameApplicationBuilder AddSceneModel<TModel>(Func<TModel> factory)
            where TModel : class, IModel
        {
            return AddModel(factory, EModelLifetime.Scene);
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
                                       mModels.ToArray(),
                                       mFlows.ToArray(),
                                       mEntityComponents.ToArray(),
                                       mEntitySystems.ToArray(),
                                       mEntityInitialCapacity,
                                       mInitialSceneModuleType,
                                       mLoggerFactory);
        }

        /// <summary>
        /// Adds one unique model registration.
        /// </summary>
        /// <typeparam name="TModel">The concrete model type.</typeparam>
        /// <param name="factory">The explicit model factory.</param>
        /// <param name="lifetime">The ownership lifetime for created instances.</param>
        /// <returns>This builder so additional configuration can be chained.</returns>
        private GameApplicationBuilder AddModel<TModel>(
            Func<TModel> factory,
            EModelLifetime lifetime)
            where TModel : class, IModel
        {
            ThrowIfBuilt();
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var modelType = typeof(TModel);
            for (int i = 0, cnt = mModels.Count; i < cnt; i++)
            {
                if (mModels[i].ModelType == modelType)
                    throw new InvalidOperationException(
                        $"Model '{modelType.FullName}' is already registered.");
            }

            mModels.Add(new ModelRegistration(modelType, factory, lifetime));
            return this;
        }

        /// <summary>
        /// Adds one unique component registration for a world lifetime.
        /// </summary>
        private GameApplicationBuilder AddComponent<T>(
            EEntityWorldLifetime lifetime)
            where T : struct, IEntityComponent
        {
            ThrowIfBuilt();
            var componentType = typeof(T);
            for (int i = 0, cnt = mEntityComponents.Count; i < cnt; i++)
            {
                if (mEntityComponents[i].ComponentType == componentType &&
                    mEntityComponents[i].Lifetime == lifetime)
                {
                    throw new InvalidOperationException(
                        $"Component '{componentType.FullName}' is already registered for '{lifetime}'.");
                }
            }

            mEntityComponents.Add(new ComponentRegistration(
                componentType,
                lifetime,
                capacity => new ComponentStore<T>(capacity)));
            return this;
        }

        /// <summary>
        /// Adds one unique entity system registration for a world lifetime.
        /// </summary>
        private GameApplicationBuilder AddEntitySystem<TSystem>(
            Func<TSystem> factory,
            EEntityWorldLifetime lifetime)
            where TSystem : class, IEntitySystem
        {
            ThrowIfBuilt();
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            var systemType = typeof(TSystem);
            for (int i = 0, cnt = mEntitySystems.Count; i < cnt; i++)
            {
                if (mEntitySystems[i].SystemType == systemType &&
                    mEntitySystems[i].Lifetime == lifetime)
                {
                    throw new InvalidOperationException(
                        $"Entity system '{systemType.FullName}' is already registered for '{lifetime}'.");
                }
            }

            mEntitySystems.Add(new EntitySystemRegistration(
                systemType,
                lifetime,
                factory));
            return this;
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
