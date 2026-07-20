using System;
using System.Collections.Generic;
using Tritone.Kernel;

namespace Tritone.Entities
{
    /// <summary>
    /// Owns generation-safe entities, dense component stores, and ordered entity systems.
    /// </summary>
    public sealed class EntityWorld : IDisposable
    {
        // Maps explicitly registered component types to typed dense stores.
        private readonly Dictionary<Type, IComponentStore> mStores;

        // Stores component stores in registration order for entity destruction.
        private readonly IComponentStore[] mStoreOrder;

        // Stores systems in stable execution order.
        private readonly IEntitySystem[] mSystems;

        // Stores current generations by entity slot.
        private int[] mGenerations;

        // Stores active state by entity slot.
        private bool[] mAlive;

        // Stores reusable entity slots.
        private int[] mFreeIndices;

        // Stores the number of reusable entity slots.
        private int mFreeCount;

        // Stores the first never-used entity slot.
        private int mNextIndex;

        // Indicates whether this world has completed disposal.
        private bool mDisposed;

        /// <summary>
        /// Initializes one world from validated component and system registrations.
        /// </summary>
        /// <param name="components">The component registrations for this world.</param>
        /// <param name="systems">The system registrations for this world.</param>
        /// <param name="initialCapacity">The reserved entity capacity.</param>
        internal EntityWorld(ComponentRegistration[] components,
                             EntitySystemRegistration[] systems,
                             int initialCapacity)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));
            if (systems == null)
                throw new ArgumentNullException(nameof(systems));
            if (initialCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            mGenerations = new int[initialCapacity];
            mAlive       = new bool[initialCapacity];
            mFreeIndices = new int[initialCapacity];
            mStores      = new Dictionary<Type, IComponentStore>(components.Length);
            mStoreOrder  = new IComponentStore[components.Length];
            for (int i = 0, cnt = components.Length; i < cnt; i++)
            {
                var store = components[i].Factory(initialCapacity);
                mStores.Add(components[i].ComponentType, store);
                mStoreOrder[i] = store;
            }

            mSystems = CreateSystems(systems);
        }

        /// <summary>
        /// Gets the number of living entities.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Creates one entity using a fresh or recycled generation-safe slot.
        /// </summary>
        /// <returns>The created entity identifier.</returns>
        public EntityId Create()
        {
            ThrowIfDisposed();
            int index;
            if (mFreeCount > 0)
                index = mFreeIndices[--mFreeCount];
            else
            {
                index = mNextIndex++;
                EnsureCapacity(mNextIndex);
            }

            if (mGenerations[index] == 0)
                mGenerations[index] = 1;
            mAlive[index] = true;
            Count++;
            return new EntityId(index, mGenerations[index]);
        }

        /// <summary>
        /// Destroys one living entity and removes all of its components.
        /// </summary>
        /// <param name="entity">The entity identifier to destroy.</param>
        /// <returns>True when a living entity was destroyed; otherwise, false.</returns>
        public bool Destroy(EntityId entity)
        {
            ThrowIfDisposed();
            if (!IsAlive(entity))
                return false;

            for (int i = 0, cnt = mStoreOrder.Length; i < cnt; i++)
                mStoreOrder[i].Remove(entity.Index);

            mAlive[entity.Index] = false;
            var generation = mGenerations[entity.Index] + 1;
            mGenerations[entity.Index] = generation > 0 ? generation : 1;
            mFreeIndices[mFreeCount++] = entity.Index;
            Count--;
            return true;
        }

        /// <summary>
        /// Determines whether one identifier still represents a living entity.
        /// </summary>
        /// <param name="entity">The entity identifier to validate.</param>
        /// <returns>True when the slot and generation are active; otherwise, false.</returns>
        public bool IsAlive(EntityId entity)
        {
            return entity.Index >= 0 &&
                   entity.Index < mNextIndex &&
                   mAlive[entity.Index] &&
                   mGenerations[entity.Index] == entity.Generation;
        }

        /// <summary>
        /// Adds one registered component to a living entity.
        /// </summary>
        /// <typeparam name="T">The registered component type.</typeparam>
        /// <param name="entity">The owning living entity.</param>
        /// <param name="component">The initial component value.</param>
        /// <returns>A reference to the stored component.</returns>
        public ref T Add<T>(EntityId entity, in T component)
            where T : struct, IEntityComponent
        {
            Validate(entity);
            return ref GetStore<T>().Add(entity.Index, in component);
        }

        /// <summary>
        /// Determines whether a living entity owns one registered component.
        /// </summary>
        /// <typeparam name="T">The registered component type.</typeparam>
        /// <param name="entity">The living entity identifier.</param>
        /// <returns>True when the component exists; otherwise, false.</returns>
        public bool Has<T>(EntityId entity) where T : struct, IEntityComponent
        {
            return IsAlive(entity) && GetStore<T>().Has(entity.Index);
        }

        /// <summary>
        /// Gets one required component from a living entity.
        /// </summary>
        /// <typeparam name="T">The registered component type.</typeparam>
        /// <param name="entity">The living entity identifier.</param>
        /// <returns>A reference to the stored component.</returns>
        public ref T Get<T>(EntityId entity) where T : struct, IEntityComponent
        {
            Validate(entity);
            return ref GetStore<T>().Get(entity.Index);
        }

        /// <summary>
        /// Removes one component from a living entity.
        /// </summary>
        /// <typeparam name="T">The registered component type.</typeparam>
        /// <param name="entity">The living entity identifier.</param>
        /// <returns>True when the component existed; otherwise, false.</returns>
        public bool Remove<T>(EntityId entity) where T : struct, IEntityComponent
        {
            Validate(entity);
            var store = GetStore<T>();
            if (!store.Has(entity.Index))
                return false;
            store.Remove(entity.Index);
            return true;
        }

        /// <summary>
        /// Creates one allocation-free query for a registered component type.
        /// </summary>
        public EntityQuery<T> Query<T>() where T : struct, IEntityComponent
        {
            ThrowIfDisposed();
            return new EntityQuery<T>(this, GetStore<T>());
        }

        /// <summary>
        /// Creates one allocation-free query for two registered component types.
        /// </summary>
        public EntityQuery<T1, T2> Query<T1, T2>()
            where T1 : struct, IEntityComponent
            where T2 : struct, IEntityComponent
        {
            ThrowIfDisposed();
            return new EntityQuery<T1, T2>(this,
                                           GetStore<T1>(),
                                           GetStore<T2>());
        }

        /// <summary>
        /// Updates every registered entity system in stable order.
        /// </summary>
        /// <param name="time">The immutable timing data for the current frame.</param>
        internal void Update(in FrameTime time)
        {
            for (int i = 0, cnt = mSystems.Length; i < cnt; i++)
                mSystems[i].Update(in time);
        }

        /// <summary>
        /// Releases systems in reverse order and clears every entity and component.
        /// </summary>
        public void Dispose()
        {
            if (mDisposed)
                return;
            mDisposed = true;

            List<Exception> errors = null;
            for (int i = mSystems.Length - 1; i >= 0; i--)
            {
                try
                {
                    mSystems[i].Shutdown();
                }
                catch (Exception exception)
                {
                    errors ??= new List<Exception>();
                    errors.Add(exception);
                }
            }
            for (int i = 0, cnt = mStoreOrder.Length; i < cnt; i++)
                mStoreOrder[i].Clear();
            Array.Clear(mAlive, 0, mNextIndex);
            Count      = 0;
            mFreeCount = 0;
            mNextIndex = 0;

            if (errors != null)
                throw new AggregateException(
                    "One or more entity systems failed to shut down.",
                    errors);
        }

        /// <summary>
        /// Gets a current identifier for one known living slot.
        /// </summary>
        /// <param name="entityIndex">The zero-based living entity slot.</param>
        /// <returns>The current entity identifier.</returns>
        internal EntityId GetEntityId(int entityIndex)
        {
            return new EntityId(entityIndex, mGenerations[entityIndex]);
        }

        /// <summary>
        /// Creates and initializes systems in stable order.
        /// </summary>
        /// <param name="registrations">The system registrations for this world.</param>
        /// <returns>The initialized systems in update order.</returns>
        private IEntitySystem[] CreateSystems(EntitySystemRegistration[] registrations)
        {
            IEntitySystem[] systems = new IEntitySystem[registrations.Length];
            var count = 0;
            try
            {
                for (int i = 0, cnt = registrations.Length; i < cnt; i++)
                {
                    var system = registrations[i].Factory();
                    if (system == null || system.GetType() != registrations[i].SystemType)
                        throw new InvalidOperationException(
                            $"Entity system factory for '{registrations[i].SystemType.FullName}' returned an invalid instance.");

                    var insertionIndex = count;
                    while (insertionIndex > 0 &&
                           systems[insertionIndex - 1].Order > system.Order)
                    {
                        systems[insertionIndex] = systems[insertionIndex - 1];
                        insertionIndex--;
                    }
                    systems[insertionIndex] = system;
                    count++;
                }

                for (int i = 0; i < count; i++)
                    systems[i].Initialize(this);
                return systems;
            }
            catch
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    try
                    {
                        systems[i]?.Shutdown();
                    }
                    catch
                    {
                    }
                }
                throw;
            }
        }

        /// <summary>
        /// Gets one explicitly registered typed component store.
        /// </summary>
        private ComponentStore<T> GetStore<T>() where T : struct, IEntityComponent
        {
            if (!mStores.TryGetValue(typeof(T), out var store))
                throw new InvalidOperationException(
                    $"Component '{typeof(T).FullName}' is not registered for this entity world.");
            return (ComponentStore<T>)store;
        }

        /// <summary>
        /// Validates one living entity identifier.
        /// </summary>
        /// <param name="entity">The entity identifier to validate.</param>
        private void Validate(EntityId entity)
        {
            ThrowIfDisposed();
            if (!IsAlive(entity))
                throw new InvalidOperationException(
                    $"Entity '{entity}' is not alive in this world.");
        }

        /// <summary>
        /// Grows entity and sparse component storage geometrically.
        /// </summary>
        /// <param name="capacity">The required entity capacity.</param>
        private void EnsureCapacity(int capacity)
        {
            if (capacity <= mGenerations.Length)
                return;
            var next = Math.Max(capacity, mGenerations.Length * 2);
            Array.Resize(ref mGenerations, next);
            Array.Resize(ref mAlive, next);
            Array.Resize(ref mFreeIndices, next);
            for (int i = 0, cnt = mStoreOrder.Length; i < cnt; i++)
                mStoreOrder[i].EnsureCapacity(next);
        }

        /// <summary>
        /// Rejects access after this world has completed disposal.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (mDisposed)
                throw new ObjectDisposedException(nameof(EntityWorld));
        }
    }
}
