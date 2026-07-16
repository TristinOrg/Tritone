using System;
using System.Collections.Generic;
using Tritone.Pooling;

namespace Tritone.Unity.Pooling
{
    /// <summary>
    /// Tracks objects borrowed by one module or Unity component lifetime.
    /// </summary>
    internal sealed class PoolScope : IPoolScope
    {
        // Stores borrowed objects in compact removal order.
        private readonly List<object> mInstances = new();

        // Maps borrowed object identities to their list indices.
        private readonly Dictionary<object, int> mIndices = new(ReferenceEqualityComparer.Instance);

        // Stores the shared pool module.
        private PoolModule mPoolModule;

        /// <summary>
        /// Initializes one ownership scope backed by the shared pool module.
        /// </summary>
        internal PoolScope(PoolModule poolModule)
        {
            mPoolModule = poolModule ?? throw new ArgumentNullException(nameof(poolModule));
        }

        /// <inheritdoc />
        public T Rent<T>() where T : class, new()
        {
            ThrowIfDisposed();
            return mPoolModule.Rent<T>(this);
        }

        /// <inheritdoc />
        public bool Return<T>(T instance) where T : class
        {
            return mPoolModule != null && mPoolModule.Return(this, instance);
        }

        /// <inheritdoc />
        public T Spawn<T>(T prefab, object parent = null) where T : class
        {
            ThrowIfDisposed();
            return mPoolModule.Spawn(this, prefab, parent);
        }

        /// <inheritdoc />
        public bool Despawn<T>(T instance) where T : class
        {
            return mPoolModule != null && mPoolModule.Despawn(this, instance);
        }

        /// <summary>
        /// Returns every object that remains owned by this scope.
        /// </summary>
        public void Dispose()
        {
            if (mPoolModule == null)
                return;

            while (mInstances.Count > 0)
                mPoolModule.ReturnOwned(this, mInstances[mInstances.Count - 1]);
            mPoolModule = null;
        }

        /// <summary>
        /// Adds one borrowed object to compact ownership tracking.
        /// </summary>
        internal void Track(object instance)
        {
            if (mIndices.ContainsKey(instance))
                throw new InvalidOperationException("The object is already tracked by this pool scope.");

            mIndices.Add(instance, mInstances.Count);
            mInstances.Add(instance);
        }

        /// <summary>
        /// Removes one returned object from compact ownership tracking.
        /// </summary>
        internal void Untrack(object instance)
        {
            if (!mIndices.TryGetValue(instance, out var index))
                return;

            var lastIndex = mInstances.Count - 1;
            var last      = mInstances[lastIndex];
            mIndices.Remove(instance);
            if (index != lastIndex)
            {
                mInstances[index] = last;
                mIndices[last]    = index;
            }
            mInstances.RemoveAt(lastIndex);
        }

        /// <summary>
        /// Rejects new rentals after this scope has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (mPoolModule == null)
                throw new ObjectDisposedException(nameof(PoolScope));
        }
    }
}
