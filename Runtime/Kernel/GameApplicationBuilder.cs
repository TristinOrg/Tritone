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
        private readonly List<ModuleRegistration> mModules = new List<ModuleRegistration>();

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
            var modules = ModuleGraph.Sort(mModules);
            return new GameApplication(modules);
        }
    }
}
