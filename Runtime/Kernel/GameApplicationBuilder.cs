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
        /// Stores the application logger factory or the default no-op implementation.
        /// </summary>
        private IModuleLoggerFactory mLoggerFactory = NullModuleLoggerFactory.Instance;

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
        /// Validates the module graph and creates an application instance.
        /// </summary>
        /// <returns>A new application in the created state.</returns>
        public GameApplication Build()
        {
            ThrowIfBuilt();
            var modules = ModuleGraph.Sort(mModules);
            mBuilt = true;
            return new GameApplication(modules, mLoggerFactory);
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
