using System;
using System.Collections.Concurrent;
using Tritone.Kernel;

namespace Tritone.Dispatching
{
    /// <summary>
    /// Executes bounded cross-thread callbacks during the application pre-update stage.
    /// </summary>
    public sealed class MainThreadDispatcherModule : ModuleBase, IMainThreadDispatcherService, IPreUpdateSystem
    {
        /// <summary>
        /// Defines the default callback safety limit for one application frame.
        /// </summary>
        private const int DefaultMaxCallbacksPerUpdate = 4096;

        /// <summary>
        /// Stores callbacks posted from any producer thread.
        /// </summary>
        private readonly ConcurrentQueue<DispatchEntry> mQueue = new();

        /// <summary>
        /// Synchronizes producer registration with deterministic dispatcher shutdown.
        /// </summary>
        private readonly object mLifecycleLock = new();

        /// <summary>
        /// Stores the maximum callbacks invoked during one pre-update.
        /// </summary>
        private readonly int mMaxCallbacksPerUpdate;

        /// <summary>
        /// Stores the dispatcher execution order.
        /// </summary>
        private readonly int mOrder;

        /// <summary>
        /// Produces unique positive callback identifiers across producer threads.
        /// </summary>
        private long mNextId;

        /// <summary>
        /// Indicates that the dispatcher permanently stopped accepting work.
        /// </summary>
        private bool mStopped;

        /// <summary>
        /// Initializes one bounded main-thread dispatcher.
        /// </summary>
        /// <param name="maxCallbacksPerUpdate">The callback safety limit for one application frame.</param>
        /// <param name="order">The execution order in the pre-update stage.</param>
        public MainThreadDispatcherModule(int maxCallbacksPerUpdate = DefaultMaxCallbacksPerUpdate,
                                          int order = -10000)
        {
            if (maxCallbacksPerUpdate < 1)
                throw new ArgumentOutOfRangeException(nameof(maxCallbacksPerUpdate));

            mMaxCallbacksPerUpdate = maxCallbacksPerUpdate;
            mOrder                 = order;
        }

        /// <inheritdoc />
        public int Order => mOrder;

        /// <inheritdoc />
        public IMainThreadDispatchScope CreateScope()
        {
            lock (mLifecycleLock)
            {
                ThrowIfStopped();
                return new MainThreadDispatchScope(this);
            }
        }

        /// <inheritdoc />
        public void PreUpdate(in FrameTime time)
        {
            var remaining = mMaxCallbacksPerUpdate;
            while (remaining > 0 && mQueue.TryDequeue(out var entry))
            {
                if (!entry.Owner.TryClaim(entry.Id))
                    continue;
                try
                {
                    entry.Callback.Invoke();
                }
                catch (Exception exception)
                {
                    Logger.Error($"Main-thread callback failed. Id: {entry.Id}.", exception);
                }
                remaining--;
            }

            if (remaining == 0 && !mQueue.IsEmpty)
                Logger.Warning($"Main-thread callback limit reached. Limit: {mMaxCallbacksPerUpdate}.");
        }

        /// <inheritdoc />
        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton<IMainThreadDispatcherService>(this);
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            lock (mLifecycleLock)
            {
                mStopped = true;
                while (mQueue.TryDequeue(out _))
                {
                }
            }
        }

        /// <summary>
        /// Registers and enqueues one callback while its owner lock prevents disposal races.
        /// </summary>
        /// <param name="owner">The scope owning the callback.</param>
        /// <param name="callback">The callback invoked during pre-update.</param>
        /// <returns>The immutable pending callback handle.</returns>
        internal DispatchHandle Post(MainThreadDispatchScope owner, Action callback)
        {
            lock (mLifecycleLock)
            {
                ThrowIfStopped();
                mNextId++;
                var id = mNextId;
                if (id <= 0)
                    throw new InvalidOperationException("Main-thread dispatch identifiers are exhausted.");
                owner.Register(id);
                mQueue.Enqueue(new DispatchEntry(id, callback, owner));
                return new DispatchHandle(id);
            }
        }

        /// <summary>
        /// Rejects new scopes and callbacks after deterministic shutdown.
        /// </summary>
        private void ThrowIfStopped()
        {
            if (mStopped)
                throw new ObjectDisposedException(nameof(MainThreadDispatcherModule));
        }
    }
}
