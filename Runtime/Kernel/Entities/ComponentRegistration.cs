using System;

namespace Tritone.Entities
{
    /// <summary>
    /// Stores one explicit component storage factory as an immutable value.
    /// </summary>
    internal readonly struct ComponentRegistration
    {
        /// <summary>
        /// Stores the concrete component type.
        /// </summary>
        internal readonly Type ComponentType;

        /// <summary>
        /// Stores the lifetime of the world receiving this component.
        /// </summary>
        internal readonly EEntityWorldLifetime Lifetime;

        /// <summary>
        /// Stores the factory that creates typed component storage.
        /// </summary>
        internal readonly Func<int, IComponentStore> Factory;

        /// <summary>
        /// Initializes one component registration.
        /// </summary>
        /// <param name="componentType">The concrete component type.</param>
        /// <param name="lifetime">The receiving world lifetime.</param>
        /// <param name="factory">The typed storage factory.</param>
        internal ComponentRegistration(Type componentType,
                                       EEntityWorldLifetime lifetime,
                                       Func<int, IComponentStore> factory)
        {
            ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
            Lifetime      = lifetime;
            Factory       = factory ?? throw new ArgumentNullException(nameof(factory));
        }
    }
}
