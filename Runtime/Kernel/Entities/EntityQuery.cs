namespace Tritone.Entities
{
    /// <summary>
    /// Provides allocation-free indexed access to entities with one component type.
    /// </summary>
    /// <typeparam name="T">The required component type.</typeparam>
    public readonly struct EntityQuery<T> where T : struct, IEntityComponent
    {
        // Stores the queried world for generation-safe identifiers.
        private readonly EntityWorld mWorld;

        // Stores dense component data.
        private readonly ComponentStore<T> mStore;

        /// <summary>
        /// Initializes one lightweight query view.
        /// </summary>
        /// <param name="world">The queried entity world.</param>
        /// <param name="store">The dense component storage.</param>
        internal EntityQuery(EntityWorld world, ComponentStore<T> store)
        {
            mWorld = world;
            mStore = store;
        }

        /// <summary>
        /// Gets the number of matching entities.
        /// </summary>
        public int Count => mStore.Count;

        /// <summary>
        /// Gets the entity identifier at one dense query index.
        /// </summary>
        /// <param name="index">The zero-based query index.</param>
        /// <returns>The matching entity identifier.</returns>
        public EntityId GetEntity(int index)
        {
            return mWorld.GetEntityId(mStore.GetEntityIndex(index));
        }

        /// <summary>
        /// Gets the component at one dense query index.
        /// </summary>
        /// <param name="index">The zero-based query index.</param>
        /// <returns>A reference to the matching component.</returns>
        public ref T GetComponent(int index)
        {
            return ref mStore.GetAt(index);
        }
    }

    /// <summary>
    /// Provides allocation-free indexed access to entities with two component types.
    /// </summary>
    public readonly struct EntityQuery<T1, T2>
        where T1 : struct, IEntityComponent
        where T2 : struct, IEntityComponent
    {
        // Stores the queried world.
        private readonly EntityWorld mWorld;

        // Stores the first component data used as the dense iteration source.
        private readonly ComponentStore<T1> mFirst;

        // Stores the second component data used for sparse membership checks.
        private readonly ComponentStore<T2> mSecond;

        /// <summary>
        /// Initializes one two-component query view.
        /// </summary>
        /// <param name="world">The queried entity world.</param>
        /// <param name="first">The first component storage.</param>
        /// <param name="second">The second component storage.</param>
        internal EntityQuery(EntityWorld world,
                             ComponentStore<T1> first,
                             ComponentStore<T2> second)
        {
            mWorld  = world;
            mFirst  = first;
            mSecond = second;
        }

        /// <summary>
        /// Gets the number of entries in the dense candidate source.
        /// </summary>
        public int CandidateCount => mFirst.Count;

        /// <summary>
        /// Attempts to get one matching entity at a candidate index.
        /// </summary>
        /// <param name="candidateIndex">The zero-based dense candidate index.</param>
        /// <param name="entity">The matching entity identifier when found.</param>
        /// <returns>True when both components exist; otherwise, false.</returns>
        public bool TryGetEntity(int candidateIndex, out EntityId entity)
        {
            var entityIndex = mFirst.GetEntityIndex(candidateIndex);
            if (!mSecond.Has(entityIndex))
            {
                entity = default;
                return false;
            }
            entity = mWorld.GetEntityId(entityIndex);
            return true;
        }

        /// <summary>
        /// Gets the first component for one matching entity.
        /// </summary>
        /// <param name="entity">The matching entity identifier.</param>
        /// <returns>A reference to the first component.</returns>
        public ref T1 GetFirst(EntityId entity)
        {
            return ref mFirst.Get(entity.Index);
        }

        /// <summary>
        /// Gets the second component for one matching entity.
        /// </summary>
        /// <param name="entity">The matching entity identifier.</param>
        /// <returns>A reference to the second component.</returns>
        public ref T2 GetSecond(EntityId entity)
        {
            return ref mSecond.Get(entity.Index);
        }
    }
}
