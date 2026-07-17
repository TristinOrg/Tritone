using System;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Content;
using Tritone.Kernel;

namespace Tritone.Unity.ContentUpdates
{
    /// <summary>
    /// Registers lifecycle-managed content updates and activates verified AssetBundle manifests.
    /// </summary>
    public sealed class ContentUpdateModule : ModuleBase, IContentUpdateService
    {
        // Executes serialized transactional content updates.
        private readonly ContentUpdater mUpdater;

        // Activates a verified manifest for future asset requests.
        private readonly Action<ContentManifest> mActivateManifest;

        // Acquires exclusive provider access before update files may change.
        private readonly Action mBeginUpdate;

        // Releases exclusive provider access after completion or failure.
        private readonly Action mEndUpdate;

        // Cancels every update when the application stops.
        private readonly CancellationTokenSource mShutdownSource = new();

        // Serializes update gates before the standalone updater lock is entered.
        private readonly SemaphoreSlim mUpdateLock = new(1, 1);

        // Indicates whether this module has permanently stopped.
        private bool mStopped;

        /// <summary>
        /// Initializes one lifecycle-managed content update module.
        /// </summary>
        /// <param name="updater">The transactional content updater.</param>
        /// <param name="beginUpdate">The callback acquiring exclusive asset provider access.</param>
        /// <param name="endUpdate">The callback releasing exclusive asset provider access.</param>
        /// <param name="activateManifest">The callback activating verified content.</param>
        public ContentUpdateModule(ContentUpdater updater,
                                   Action beginUpdate,
                                   Action endUpdate,
                                   Action<ContentManifest> activateManifest)
        {
            mUpdater          = updater ?? throw new ArgumentNullException(nameof(updater));
            mBeginUpdate      = beginUpdate ?? throw new ArgumentNullException(nameof(beginUpdate));
            mEndUpdate        = endUpdate ?? throw new ArgumentNullException(nameof(endUpdate));
            mActivateManifest = activateManifest ??
                                throw new ArgumentNullException(nameof(activateManifest));
        }

        /// <summary>
        /// Registers application-wide content update access.
        /// </summary>
        /// <param name="services">The application service registry.</param>
        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton<IContentUpdateService>(this);
        }

        /// <inheritdoc />
        public IContentUpdateScope CreateScope()
        {
            if (mStopped)
                throw new InvalidOperationException(
                    "Content update scopes cannot be created after the content update module has stopped.");

            return new ContentUpdateScope(this, mShutdownSource.Token);
        }

        /// <summary>
        /// Executes one scoped update and activates changed content before returning.
        /// </summary>
        /// <param name="progress">The optional progress callback.</param>
        /// <param name="cancellationToken">The scope-owned cancellation token.</param>
        /// <returns>A task containing the successful update result.</returns>
        internal async Task<ContentUpdateResult> UpdateAsync(Action<ContentUpdateProgress> progress,
                                                             CancellationToken cancellationToken)
        {
            if (mStopped)
                throw new ObjectDisposedException(nameof(ContentUpdateModule));

            await mUpdateLock.WaitAsync(cancellationToken);
            try
            {
                mBeginUpdate.Invoke();
                try
                {
                    var result = await mUpdater.UpdateAsync(progress, cancellationToken);
                    if (mStopped)
                        throw new ObjectDisposedException(nameof(ContentUpdateModule));
                    if (result.Updated)
                        mActivateManifest.Invoke(result.ActiveManifest);
                    return result;
                }
                finally
                {
                    mEndUpdate.Invoke();
                }
            }
            finally
            {
                mUpdateLock.Release();
            }
        }

        /// <summary>
        /// Cancels every active scope when the application stops.
        /// </summary>
        protected override void OnStop()
        {
            mStopped = true;
            mShutdownSource.Cancel();
            mShutdownSource.Dispose();
        }
    }

    /// <summary>
    /// Owns one module's active update cancellation and releases it automatically.
    /// </summary>
    internal sealed class ContentUpdateScope : IContentUpdateScope
    {
        // Stores the shared update module.
        private readonly ContentUpdateModule mModule;

        // Stores the application shutdown token.
        private readonly CancellationToken mShutdownToken;

        // Protects scope state across asynchronous continuations.
        private readonly object mSyncRoot = new();

        // Stores cancellation for the currently active update.
        private CancellationTokenSource mUpdateSource;

        // Indicates whether this scope has been released.
        private bool mDisposed;

        /// <summary>
        /// Initializes one empty content update scope.
        /// </summary>
        /// <param name="module">The shared update module.</param>
        /// <param name="shutdownToken">The application shutdown token.</param>
        internal ContentUpdateScope(ContentUpdateModule module,
                                    CancellationToken shutdownToken)
        {
            mModule        = module;
            mShutdownToken = shutdownToken;
        }

        /// <inheritdoc />
        public async Task<ContentUpdateResult> UpdateAsync(
            Action<ContentUpdateProgress> progress = null)
        {
            CancellationTokenSource updateSource;
            lock (mSyncRoot)
            {
                if (mDisposed)
                    throw new ObjectDisposedException(nameof(ContentUpdateScope));
                if (mUpdateSource != null)
                    throw new InvalidOperationException(
                        "This content update scope already owns an active update.");

                updateSource  = CancellationTokenSource.CreateLinkedTokenSource(mShutdownToken);
                mUpdateSource = updateSource;
            }

            try
            {
                return await mModule.UpdateAsync(progress, updateSource.Token);
            }
            finally
            {
                lock (mSyncRoot)
                {
                    if (ReferenceEquals(mUpdateSource, updateSource))
                        mUpdateSource = null;
                }
                updateSource.Dispose();
            }
        }

        /// <inheritdoc />
        public void Cancel()
        {
            lock (mSyncRoot)
                mUpdateSource?.Cancel();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (mSyncRoot)
            {
                if (mDisposed)
                    return;

                mDisposed = true;
                mUpdateSource?.Cancel();
            }
        }
    }
}
