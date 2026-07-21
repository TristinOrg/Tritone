using System;
using System.Collections.Generic;

namespace Tritone.Dispatching
{
    /// <summary>
    /// Tracks and cancels main-thread callbacks owned by one module lifetime.
    /// </summary>
    internal sealed class MainThreadDispatchScope : IMainThreadDispatchScope
    {
        /// <summary>
        /// Stores pending callback identifiers behind the scope synchronization lock.
        /// </summary>
        private readonly HashSet<long> mPending = new();

        /// <summary>
        /// Synchronizes posting, cancellation, execution, and disposal across threads.
        /// </summary>
        private readonly object mLock = new();

        /// <summary>
        /// Stores the dispatcher accepting work for this scope.
        /// </summary>
        private MainThreadDispatcherModule mDispatcher;

        /// <summary>
        /// Initializes one scope owned by a dispatcher module.
        /// </summary>
        /// <param name="dispatcher">The dispatcher accepting this scope's work.</param>
        internal MainThreadDispatchScope(MainThreadDispatcherModule dispatcher)
        {
            mDispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        /// <inheritdoc />
        public DispatchHandle Post(Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            lock (mLock)
            {
                if (mDispatcher == null)
                    throw new ObjectDisposedException(nameof(MainThreadDispatchScope));
                return mDispatcher.Post(this, callback);
            }
        }

        /// <inheritdoc />
        public bool Cancel(DispatchHandle handle)
        {
            if (!handle.IsValid)
                return false;
            lock (mLock)
                return mPending.Remove(handle.Id);
        }

        /// <inheritdoc />
        public bool IsPending(DispatchHandle handle)
        {
            if (!handle.IsValid)
                return false;
            lock (mLock)
                return mPending.Contains(handle.Id);
        }

        /// <inheritdoc />
        public void CancelAll()
        {
            lock (mLock)
                mPending.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (mLock)
            {
                if (mDispatcher == null)
                    return;
                mPending.Clear();
                mDispatcher = null;
            }
        }

        /// <summary>
        /// Registers one identifier before its queue entry becomes visible to the application thread.
        /// </summary>
        /// <param name="id">The unique scheduler identifier.</param>
        internal void Register(long id)
        {
            mPending.Add(id);
        }

        /// <summary>
        /// Atomically claims one callback for execution unless its scope cancelled it.
        /// </summary>
        /// <param name="id">The queued callback identifier.</param>
        /// <returns>True when the callback remained pending and may execute.</returns>
        internal bool TryClaim(long id)
        {
            lock (mLock)
                return mDispatcher != null && mPending.Remove(id);
        }
    }
}
