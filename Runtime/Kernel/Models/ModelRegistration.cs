using System;

namespace Tritone.Models
{
    /// <summary>
    /// Stores one validated model factory and its ownership lifetime.
    /// </summary>
    internal readonly struct ModelRegistration
    {
        /// <summary>
        /// Stores the concrete model type used as the lookup key.
        /// </summary>
        internal readonly Type ModelType;

        /// <summary>
        /// Stores the factory invoked when the model is first requested.
        /// </summary>
        internal readonly Func<IModel> Factory;

        /// <summary>
        /// Stores the lifetime that owns created model instances.
        /// </summary>
        internal readonly EModelLifetime Lifetime;

        /// <summary>
        /// Initializes one model registration.
        /// </summary>
        /// <param name="modelType">The concrete model type.</param>
        /// <param name="factory">The factory invoked on first access.</param>
        /// <param name="lifetime">The lifetime that owns created instances.</param>
        internal ModelRegistration(Type modelType,
                                   Func<IModel> factory,
                                   EModelLifetime lifetime)
        {
            ModelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
            Factory   = factory ?? throw new ArgumentNullException(nameof(factory));
            Lifetime  = lifetime;
        }
    }
}
