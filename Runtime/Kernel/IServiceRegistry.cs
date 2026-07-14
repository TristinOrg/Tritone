using System;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides explicit registration and lookup of application-scoped services.
    /// </summary>
    public interface IServiceRegistry
    {
        /// <summary>
        /// Registers one application-scoped service instance.
        /// </summary>
        /// <typeparam name="TService">The service contract used as the lookup key.</typeparam>
        /// <param name="instance">The service instance to register.</param>
        void AddSingleton<TService>(TService instance) where TService : class;

        /// <summary>
        /// Gets a required service by its runtime type.
        /// </summary>
        /// <param name="serviceType">The service contract used as the lookup key.</param>
        /// <returns>The registered service instance.</returns>
        object GetRequired(Type serviceType);

        /// <summary>
        /// Gets a required service by its generic service contract.
        /// </summary>
        /// <typeparam name="TService">The service contract used as the lookup key.</typeparam>
        /// <returns>The registered service instance.</returns>
        TService GetRequired<TService>() where TService : class;

        /// <summary>
        /// Attempts to get an optional service by its generic service contract.
        /// </summary>
        /// <typeparam name="TService">The service contract used as the lookup key.</typeparam>
        /// <param name="service">The registered instance when found; otherwise, null.</param>
        /// <returns>True when the service exists; otherwise, false.</returns>
        bool TryGet<TService>(out TService service) where TService : class;
    }
}
