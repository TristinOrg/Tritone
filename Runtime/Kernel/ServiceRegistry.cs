using System;
using System.Collections.Generic;

namespace Tritone.Kernel
{
    /// <summary>
    /// Stores application-scoped services and prevents changes after startup.
    /// </summary>
    internal sealed class ServiceRegistry : IServiceRegistry
    {
        /// <summary>
        /// Stores service instances by their explicit service contract.
        /// </summary>
        private readonly Dictionary<Type, object> mServices = new Dictionary<Type, object>();

        /// <summary>
        /// Indicates whether service registration has been closed.
        /// </summary>
        private bool mSealed;

        /// <summary>
        /// Registers one application-scoped service instance.
        /// </summary>
        /// <typeparam name="TService">The service contract used as the lookup key.</typeparam>
        /// <param name="instance">The service instance to register.</param>
        public void AddSingleton<TService>(TService instance) where TService : class
        {
            AddSingleton(typeof(TService), instance);
        }

        /// <summary>
        /// Registers a service by its runtime contract type.
        /// </summary>
        /// <param name="serviceType">The service contract used as the lookup key.</param>
        /// <param name="instance">The service instance to register.</param>
        internal void AddSingleton(Type serviceType, object instance)
        {
            if (mSealed)
                throw new InvalidOperationException("Services cannot be changed after startup.");
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (mServices.ContainsKey(serviceType))
                throw new InvalidOperationException($"Service '{serviceType.FullName}' is already registered.");

            mServices.Add(serviceType, instance);
        }

        /// <summary>
        /// Gets a required service by its runtime type.
        /// </summary>
        /// <param name="serviceType">The service contract used as the lookup key.</param>
        /// <returns>The registered service instance.</returns>
        public object GetRequired(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (!mServices.TryGetValue(serviceType, out var service))
                throw new InvalidOperationException($"Service '{serviceType.FullName}' is not registered.");

            return service;
        }

        /// <summary>
        /// Gets a required service by its generic service contract.
        /// </summary>
        /// <typeparam name="TService">The service contract used as the lookup key.</typeparam>
        /// <returns>The registered service instance.</returns>
        public TService GetRequired<TService>() where TService : class
        {
            return (TService)GetRequired(typeof(TService));
        }

        /// <summary>
        /// Attempts to get an optional service by its generic service contract.
        /// </summary>
        /// <typeparam name="TService">The service contract used as the lookup key.</typeparam>
        /// <param name="service">The registered instance when found; otherwise, null.</param>
        /// <returns>True when the service exists; otherwise, false.</returns>
        public bool TryGet<TService>(out TService service) where TService : class
        {
            if (mServices.TryGetValue(typeof(TService), out var value))
            {
                service = (TService)value;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Closes registration before application modules start.
        /// </summary>
        internal void Seal()
        {
            mSealed = true;
        }
    }
}
