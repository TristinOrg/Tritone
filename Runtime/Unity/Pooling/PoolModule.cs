using System;
using System.Collections.Generic;
using Tritone.Kernel;
using Tritone.Pooling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tritone.Unity.Pooling
{
    /// <summary>
    /// Provides lazy plain-object and Unity-prefab pools shared by the application.
    /// </summary>
    public sealed class PoolModule : ModuleBase, IPoolService
    {
        // Maps plain object types to their lazy pools.
        private readonly Dictionary<Type, IObjectPool> mObjectPools = new();

        // Maps active plain objects to their source pools.
        private readonly Dictionary<object, IObjectPool> mActiveObjectPools = new(ReferenceEqualityComparer.Instance);

        // Maps active plain objects to their ownership scopes.
        private readonly Dictionary<object, PoolScope> mActiveObjectOwners = new(ReferenceEqualityComparer.Instance);

        // Maps prefab instance IDs to their lazy pools.
        private readonly Dictionary<int, PrefabPool> mPrefabPools = new();

        // Maps active Unity values to their instance records.
        private readonly Dictionary<object, PooledPrefabInstance> mActivePrefabInstances = new(ReferenceEqualityComparer.Instance);

        // Maps active Unity values to their source pools.
        private readonly Dictionary<object, PrefabPool> mActivePrefabPools = new(ReferenceEqualityComparer.Instance);

        // Maps active Unity values to their ownership scopes.
        private readonly Dictionary<object, PoolScope> mActivePrefabOwners = new(ReferenceEqualityComparer.Instance);

        // Stores initial storage reserved by every lazy pool.
        private readonly int mDefaultCapacity;

        // Stores the maximum inactive objects retained by every lazy pool.
        private readonly int mDefaultMaxCapacity;

        // Stores the hidden parent for inactive Unity objects.
        private Transform mStorageRoot;

        // Indicates whether this module has permanently stopped.
        private bool mStopped;

        /// <summary>
        /// Initializes shared lazy pools with default capacity limits.
        /// </summary>
        public PoolModule(int defaultCapacity = 8, int defaultMaxCapacity = 128)
        {
            if (defaultCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(defaultCapacity));
            if (defaultMaxCapacity < 1 || defaultMaxCapacity < defaultCapacity)
                throw new ArgumentOutOfRangeException(nameof(defaultMaxCapacity));

            mDefaultCapacity    = defaultCapacity;
            mDefaultMaxCapacity = defaultMaxCapacity;
        }

        /// <summary>
        /// Registers pool access and creates inactive Unity storage.
        /// </summary>
        protected override void OnConfigure(IServiceRegistry services)
        {
            var storageObject = new GameObject("Tritone.Pools");
            storageObject.SetActive(false);
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(storageObject);
            mStorageRoot = storageObject.transform;
            services.AddSingleton<IPoolService>(this);
        }

        /// <inheritdoc />
        public IPoolScope CreateScope()
        {
            if (mStopped)
                throw new InvalidOperationException("Pool scopes cannot be created after the pool module has stopped.");
            return new PoolScope(this);
        }

        /// <summary>
        /// Rents one plain object for a specific ownership scope.
        /// </summary>
        internal T Rent<T>(PoolScope owner) where T : class, new()
        {
            var objectType = typeof(T);
            if (!mObjectPools.TryGetValue(objectType, out var pool))
            {
                pool = new ObjectPool<T>(mDefaultCapacity, mDefaultMaxCapacity);
                mObjectPools.Add(objectType, pool);
            }

            var instance = (T)pool.Rent();
            mActiveObjectPools.Add(instance, pool);
            mActiveObjectOwners.Add(instance, owner);
            owner.Track(instance);
            return instance;
        }

        /// <summary>
        /// Returns one plain object owned by the supplied scope.
        /// </summary>
        internal bool Return(PoolScope owner, object instance)
        {
            if (instance == null ||
                !mActiveObjectOwners.TryGetValue(instance, out var activeOwner) ||
                !ReferenceEquals(owner, activeOwner))
                return false;

            var pool = mActiveObjectPools[instance];
            owner.Untrack(instance);
            mActiveObjectOwners.Remove(instance);
            mActiveObjectPools.Remove(instance);
            pool.Return(instance);
            return true;
        }

        /// <summary>
        /// Spawns one Unity prefab for a specific ownership scope.
        /// </summary>
        internal T Spawn<T>(PoolScope owner, T prefab, object parent) where T : class
        {
            if (prefab is not Object unityPrefab || unityPrefab == null)
                throw new ArgumentException("Spawn requires a Unity GameObject or Component prefab.", nameof(prefab));
            if (unityPrefab is not GameObject && unityPrefab is not Component)
                throw new ArgumentException("Spawn supports only GameObject and Component prefabs.", nameof(prefab));
            if (parent != null && parent is not Transform)
                throw new ArgumentException("A prefab parent must be a Unity Transform.", nameof(parent));

            var prefabId = unityPrefab.GetInstanceID();
            if (!mPrefabPools.TryGetValue(prefabId, out var pool))
            {
                pool = new PrefabPool(unityPrefab,
                                      mStorageRoot,
                                      mDefaultCapacity,
                                      mDefaultMaxCapacity);
                mPrefabPools.Add(prefabId, pool);
            }
            else if (pool.Prefab != unityPrefab)
                throw new InvalidOperationException("A Unity instance ID collision was detected between prefab pools.");

            var pooledInstance = pool.Spawn(parent as Transform);
            var instance       = (T)pooledInstance.Value;
            mActivePrefabInstances.Add(instance, pooledInstance);
            mActivePrefabPools.Add(instance, pool);
            mActivePrefabOwners.Add(instance, owner);
            owner.Track(instance);
            return instance;
        }

        /// <summary>
        /// Returns one Unity object owned by the supplied scope.
        /// </summary>
        internal bool Despawn(PoolScope owner, object instance)
        {
            if (instance == null ||
                !mActivePrefabOwners.TryGetValue(instance, out var activeOwner) ||
                !ReferenceEquals(owner, activeOwner))
                return false;

            var pooledInstance = mActivePrefabInstances[instance];
            var pool           = mActivePrefabPools[instance];
            owner.Untrack(instance);
            mActivePrefabOwners.Remove(instance);
            mActivePrefabInstances.Remove(instance);
            mActivePrefabPools.Remove(instance);
            if (pooledInstance.GameObject != null)
                pool.Despawn(pooledInstance);
            return true;
        }

        /// <summary>
        /// Returns one object without requiring the scope to know its pool category.
        /// </summary>
        internal void ReturnOwned(PoolScope owner, object instance)
        {
            if (mActivePrefabOwners.ContainsKey(instance))
            {
                Despawn(owner, instance);
                return;
            }
            if (mActiveObjectOwners.ContainsKey(instance))
            {
                Return(owner, instance);
                return;
            }

            owner.Untrack(instance);
        }

        /// <summary>
        /// Releases every active and inactive pooled object.
        /// </summary>
        protected override void OnStop()
        {
            mStopped = true;

            foreach (var pair in mActivePrefabInstances)
            {
                var instance = pair.Value;
                if (instance.GameObject == null)
                    continue;
                for (int i = 0, cnt = instance.Callbacks.Length; i < cnt; i++)
                    instance.Callbacks[i].OnDespawn();
                UnityObjectUtility.Destroy(instance.GameObject);
            }
            foreach (var pair in mPrefabPools)
                pair.Value.Clear();
            foreach (var pair in mActiveObjectPools)
            {
                if (pair.Key is IPoolable poolable)
                    poolable.OnDespawn();
            }
            foreach (var pair in mObjectPools)
                pair.Value.Clear();

            mActivePrefabInstances.Clear();
            mActivePrefabPools.Clear();
            mActivePrefabOwners.Clear();
            mPrefabPools.Clear();
            mActiveObjectPools.Clear();
            mActiveObjectOwners.Clear();
            mObjectPools.Clear();

            if (mStorageRoot != null)
                UnityObjectUtility.Destroy(mStorageRoot.gameObject);
            mStorageRoot = null;
        }
    }
}
