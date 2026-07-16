using System;
using System.Collections.Generic;
using Tritone.Pooling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tritone.Unity.Pooling
{
    /// <summary>
    /// Stores reusable instances created from one Unity prefab.
    /// </summary>
    internal sealed class PrefabPool
    {
        // Stores inactive prefab instances.
        private readonly Stack<PooledPrefabInstance> mAvailable;

        // Stores the prefab used to create new instances.
        private readonly Object mPrefab;

        // Stores the prefab GameObject used by Unity Instantiate.
        private readonly GameObject mPrefabObject;

        // Stores the returned component type, or GameObject for GameObject prefabs.
        private readonly Type mValueType;

        // Stores the hidden parent used by inactive instances.
        private readonly Transform mStorageRoot;

        // Stores the maximum number of inactive instances retained by this pool.
        private readonly int mMaxCapacity;

        // Stores the prefab local position restored on every spawn.
        private readonly Vector3 mLocalPosition;

        // Stores the prefab local rotation restored on every spawn.
        private readonly Quaternion mLocalRotation;

        // Stores the prefab local scale restored on every spawn.
        private readonly Vector3 mLocalScale;

        /// <summary>
        /// Initializes one lazy prefab pool.
        /// </summary>
        internal PrefabPool(Object prefab, Transform storageRoot, int capacity, int maxCapacity)
        {
            mPrefab       = prefab;
            mStorageRoot  = storageRoot;
            mAvailable    = new(capacity);
            mMaxCapacity  = maxCapacity;
            mPrefabObject = prefab is GameObject gameObject
                ? gameObject
                : ((Component)prefab).gameObject;
            mValueType     = prefab.GetType();
            mLocalPosition = mPrefabObject.transform.localPosition;
            mLocalRotation = mPrefabObject.transform.localRotation;
            mLocalScale    = mPrefabObject.transform.localScale;
        }

        // Gets the source prefab used as this pool identity.
        internal Object Prefab => mPrefab;

        /// <summary>
        /// Spawns one cached or newly instantiated prefab object.
        /// </summary>
        internal PooledPrefabInstance Spawn(Transform parent)
        {
            PooledPrefabInstance instance = null;
            while (mAvailable.Count > 0 && instance == null)
            {
                instance = mAvailable.Pop();
                if (instance.GameObject == null)
                    instance = null;
            }

            instance ??= CreateInstance();
            var transform = instance.GameObject.transform;
            transform.SetParent(parent, false);
            transform.localPosition = mLocalPosition;
            transform.localRotation = mLocalRotation;
            transform.localScale    = mLocalScale;
            instance.GameObject.SetActive(true);
            for (int i = 0, cnt = instance.Callbacks.Length; i < cnt; i++)
                instance.Callbacks[i].OnSpawn();
            return instance;
        }

        /// <summary>
        /// Returns one active prefab instance to inactive storage.
        /// </summary>
        internal void Despawn(PooledPrefabInstance instance)
        {
            for (int i = 0, cnt = instance.Callbacks.Length; i < cnt; i++)
                instance.Callbacks[i].OnDespawn();

            instance.GameObject.SetActive(false);
            if (mAvailable.Count >= mMaxCapacity)
            {
                UnityObjectUtility.Destroy(instance.GameObject);
                return;
            }

            instance.GameObject.transform.SetParent(mStorageRoot, false);
            mAvailable.Push(instance);
        }

        /// <summary>
        /// Destroys every inactive prefab instance.
        /// </summary>
        internal void Clear()
        {
            while (mAvailable.Count > 0)
            {
                var instance = mAvailable.Pop();
                if (instance.GameObject != null)
                    UnityObjectUtility.Destroy(instance.GameObject);
            }
        }

        /// <summary>
        /// Creates one prefab instance and caches its pool callbacks.
        /// </summary>
        private PooledPrefabInstance CreateInstance()
        {
            var gameObject = Object.Instantiate(mPrefabObject, mStorageRoot, false);
            Object value = mValueType == typeof(GameObject)
                ? gameObject
                : gameObject.GetComponent(mValueType);
            if (value == null)
            {
                UnityObjectUtility.Destroy(gameObject);
                throw new InvalidOperationException($"Prefab {mPrefabObject.name} does not contain {mValueType.Name}.");
            }

            var behaviours = gameObject.GetComponents<MonoBehaviour>();
            List<IPoolable> callbacks = null;
            for (int i = 0, cnt = behaviours.Length; i < cnt; i++)
            {
                if (behaviours[i] is not IPoolable poolable)
                    continue;
                callbacks ??= new();
                callbacks.Add(poolable);
            }

            return new PooledPrefabInstance(gameObject,
                                            value,
                                            callbacks?.ToArray() ?? Array.Empty<IPoolable>());
        }
    }

    /// <summary>
    /// Stores one prefab instance and its cached lifecycle callbacks.
    /// </summary>
    internal sealed class PooledPrefabInstance
    {
        // Stores the instantiated Unity GameObject.
        internal readonly GameObject GameObject;

        // Stores the GameObject or Component returned to the caller.
        internal readonly object Value;

        // Stores cached callbacks discovered when the instance was created.
        internal readonly IPoolable[] Callbacks;

        /// <summary>
        /// Initializes one prefab instance record.
        /// </summary>
        internal PooledPrefabInstance(GameObject gameObject, object value, IPoolable[] callbacks)
        {
            GameObject = gameObject;
            Value      = value;
            Callbacks  = callbacks;
        }
    }
}
