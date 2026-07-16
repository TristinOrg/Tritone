using System;
using System.Collections.Generic;
using Tritone.Pooling;

namespace Tritone.Unity.Pooling
{
    /// <summary>
    /// Provides non-generic storage operations used by the shared pool module.
    /// </summary>
    internal interface IObjectPool
    {
        /// <summary>
        /// Rents one object from this pool.
        /// </summary>
        /// <returns>A newly created or cached object.</returns>
        object Rent();

        /// <summary>
        /// Returns one object to this pool.
        /// </summary>
        /// <param name="instance">The object to cache or discard.</param>
        void Return(object instance);

        /// <summary>
        /// Releases every cached object reference.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Stores reusable plain C# objects of one concrete type.
    /// </summary>
    /// <typeparam name="T">The pooled object type.</typeparam>
    internal sealed class ObjectPool<T> : IObjectPool where T : class, new()
    {
        // Stores currently available objects.
        private readonly Stack<T> mAvailable;

        // Stores the maximum number of inactive objects retained by this pool.
        private readonly int mMaxCapacity;

        /// <summary>
        /// Initializes one type pool with preallocated stack storage.
        /// </summary>
        internal ObjectPool(int capacity, int maxCapacity)
        {
            mAvailable   = new(capacity);
            mMaxCapacity = maxCapacity;
        }

        /// <inheritdoc />
        public object Rent()
        {
            var instance = mAvailable.Count > 0 ? mAvailable.Pop() : new T();
            if (instance is IPoolable poolable)
                poolable.OnSpawn();
            return instance;
        }

        /// <inheritdoc />
        public void Return(object instance)
        {
            var value = (T)instance;
            if (value is IPoolable poolable)
                poolable.OnDespawn();
            if (mAvailable.Count < mMaxCapacity)
                mAvailable.Push(value);
        }

        /// <inheritdoc />
        public void Clear()
        {
            mAvailable.Clear();
        }
    }
}
