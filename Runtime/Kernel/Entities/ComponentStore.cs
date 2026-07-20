using System;

namespace Tritone.Entities
{
    /// <summary>
    /// Provides non-generic operations required by entity destruction and world growth.
    /// </summary>
    internal interface IComponentStore
    {
        /// <summary>
        /// Ensures sparse storage can address every entity slot below the capacity.
        /// </summary>
        /// <param name="capacity">The required entity slot capacity.</param>
        void EnsureCapacity(int capacity);

        /// <summary>
        /// Removes one entity component when present.
        /// </summary>
        /// <param name="entityIndex">The zero-based entity slot index.</param>
        void Remove(int entityIndex);

        /// <summary>
        /// Clears all component data while retaining allocated storage.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Stores one component type in dense arrays with sparse entity lookup.
    /// </summary>
    /// <typeparam name="T">The registered component value type.</typeparam>
    internal sealed class ComponentStore<T> : IComponentStore
        where T : struct, IEntityComponent
    {
        // Maps entity indices to dense indices plus one, with zero representing absence.
        private int[] mSparse;

        // Stores component owners in dense order.
        private int[] mEntities;

        // Stores component values in dense order.
        private T[] mComponents;

        /// <summary>
        /// Initializes component storage with one reserved capacity.
        /// </summary>
        /// <param name="capacity">The initial entity and component capacity.</param>
        internal ComponentStore(int capacity)
        {
            mSparse     = new int[capacity];
            mEntities   = new int[capacity];
            mComponents = new T[capacity];
        }

        /// <summary>
        /// Gets the number of stored components.
        /// </summary>
        internal int Count { get; private set; }

        /// <summary>
        /// Determines whether one entity owns this component type.
        /// </summary>
        /// <param name="entityIndex">The zero-based entity slot index.</param>
        /// <returns>True when the component exists; otherwise, false.</returns>
        internal bool Has(int entityIndex)
        {
            return entityIndex >= 0 &&
                   entityIndex < mSparse.Length &&
                   mSparse[entityIndex] != 0;
        }

        /// <summary>
        /// Adds a component and returns its stable reference until this store changes structurally.
        /// </summary>
        /// <param name="entityIndex">The zero-based owning entity slot.</param>
        /// <param name="component">The initial component value.</param>
        /// <returns>A reference to the stored component value.</returns>
        internal ref T Add(int entityIndex, in T component)
        {
            if (Has(entityIndex))
                throw new InvalidOperationException(
                    $"Entity index '{entityIndex}' already has component '{typeof(T).FullName}'.");

            EnsureDenseCapacity(Count + 1);
            var denseIndex          = Count++;
            mSparse[entityIndex]    = denseIndex + 1;
            mEntities[denseIndex]   = entityIndex;
            mComponents[denseIndex] = component;
            return ref mComponents[denseIndex];
        }

        /// <summary>
        /// Gets one required component by entity slot.
        /// </summary>
        /// <param name="entityIndex">The zero-based owning entity slot.</param>
        /// <returns>A reference to the stored component value.</returns>
        internal ref T Get(int entityIndex)
        {
            var sparseIndex = entityIndex >= 0 && entityIndex < mSparse.Length
                ? mSparse[entityIndex]
                : 0;
            if (sparseIndex == 0)
                throw new InvalidOperationException(
                    $"Entity index '{entityIndex}' does not have component '{typeof(T).FullName}'.");
            return ref mComponents[sparseIndex - 1];
        }

        /// <summary>
        /// Gets the entity slot stored at one dense index.
        /// </summary>
        /// <param name="denseIndex">The zero-based dense component index.</param>
        /// <returns>The owning entity slot index.</returns>
        internal int GetEntityIndex(int denseIndex)
        {
            return mEntities[denseIndex];
        }

        /// <summary>
        /// Gets a component by dense index.
        /// </summary>
        /// <param name="denseIndex">The zero-based dense component index.</param>
        /// <returns>A reference to the stored component.</returns>
        internal ref T GetAt(int denseIndex)
        {
            return ref mComponents[denseIndex];
        }

        /// <inheritdoc />
        public void EnsureCapacity(int capacity)
        {
            if (capacity <= mSparse.Length)
                return;
            Array.Resize(ref mSparse, capacity);
        }

        /// <inheritdoc />
        public void Remove(int entityIndex)
        {
            if (!Has(entityIndex))
                return;

            var denseIndex = mSparse[entityIndex] - 1;
            var lastIndex  = --Count;
            mSparse[entityIndex] = 0;
            if (denseIndex != lastIndex)
            {
                var movedEntity          = mEntities[lastIndex];
                mEntities[denseIndex]    = movedEntity;
                mComponents[denseIndex]  = mComponents[lastIndex];
                mSparse[movedEntity]     = denseIndex + 1;
            }
            mEntities[lastIndex]   = 0;
            mComponents[lastIndex] = default;
        }

        /// <inheritdoc />
        public void Clear()
        {
            Array.Clear(mSparse, 0, mSparse.Length);
            Array.Clear(mEntities, 0, Count);
            Array.Clear(mComponents, 0, Count);
            Count = 0;
        }

        /// <summary>
        /// Grows dense arrays geometrically when another component is added.
        /// </summary>
        /// <param name="capacity">The required dense component capacity.</param>
        private void EnsureDenseCapacity(int capacity)
        {
            if (capacity <= mComponents.Length)
                return;
            var next = Math.Max(capacity, mComponents.Length * 2);
            Array.Resize(ref mEntities, next);
            Array.Resize(ref mComponents, next);
        }
    }
}
