using System;

namespace Tritone.Entities
{
    /// <summary>
    /// Stores one explicit entity system factory and world lifetime.
    /// </summary>
    internal readonly struct EntitySystemRegistration
    {
        /// <summary>
        /// Stores the concrete system type.
        /// </summary>
        internal readonly Type SystemType;

        /// <summary>
        /// Stores the receiving world lifetime.
        /// </summary>
        internal readonly EEntityWorldLifetime Lifetime;

        /// <summary>
        /// Stores the fresh system factory.
        /// </summary>
        internal readonly Func<IEntitySystem> Factory;

        /// <summary>
        /// Initializes one entity system registration.
        /// </summary>
        /// <param name="systemType">The concrete system type.</param>
        /// <param name="lifetime">The receiving world lifetime.</param>
        /// <param name="factory">The fresh system factory.</param>
        internal EntitySystemRegistration(Type systemType,
                                          EEntityWorldLifetime lifetime,
                                          Func<IEntitySystem> factory)
        {
            SystemType = systemType ?? throw new ArgumentNullException(nameof(systemType));
            Lifetime   = lifetime;
            Factory    = factory ?? throw new ArgumentNullException(nameof(factory));
        }
    }
}
