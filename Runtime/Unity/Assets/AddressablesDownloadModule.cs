using System;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Kernel;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Serializes dependency preloads and binds them to application shutdown.
    /// </summary>
    public sealed class AddressablesDownloadModule : ModuleBase, IAddressablesDownloadService
    {
        /// <summary>
        /// Executes concrete Addressables dependency operations.
        /// </summary>
        private readonly IAddressablesDownloadBackend mBackend;

        /// <summary>
        /// Serializes size checks and downloads to avoid duplicate cache work.
        /// </summary>
        private readonly SemaphoreSlim mDownloadLock = new(1, 1);

        /// <summary>
        /// Cancels active work during application shutdown.
        /// </summary>
        private readonly CancellationTokenSource mShutdownSource = new();

        /// <summary>
        /// Indicates whether application shutdown has begun.
        /// </summary>
        private bool mStopped;

        /// <summary>
        /// Initializes dependency preloads with one explicit backend.
        /// </summary>
        /// <param name="backend">The backend executing Addressables dependency operations.</param>
        public AddressablesDownloadModule(IAddressablesDownloadBackend backend)
        {
            mBackend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        /// <summary>
        /// Registers application-wide Addressables dependency preload access.
        /// </summary>
        /// <param name="services">The application service registry.</param>
        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton<IAddressablesDownloadService>(this);
        }

        /// <inheritdoc />
        public async Task<AddressablesDownloadResult> DownloadAsync(string key, Action<AddressablesDownloadProgress> progress = null, CancellationToken cancellationToken = default)
        {
            if (mStopped)
                throw new ObjectDisposedException(nameof(AddressablesDownloadModule));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("An Addressables download key is required.", nameof(key));

            using var downloadSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, mShutdownSource.Token);
            await mDownloadLock.WaitAsync(downloadSource.Token);
            try
            {
                var downloadBytes = await mBackend.GetDownloadSizeAsync(key, downloadSource.Token);
                if (downloadBytes < 0)
                    throw new InvalidOperationException("The Addressables download backend returned a negative byte count.");
                if (downloadBytes == 0)
                {
                    progress?.Invoke(new AddressablesDownloadProgress(0, 0));
                    return new AddressablesDownloadResult(key, 0);
                }

                await mBackend.DownloadDependenciesAsync(key, progress, downloadSource.Token);
                return new AddressablesDownloadResult(key, downloadBytes);
            }
            finally
            {
                mDownloadLock.Release();
            }
        }

        /// <summary>
        /// Cancels active downloads and rejects future work.
        /// </summary>
        protected override void OnStop()
        {
            mStopped = true;
            mShutdownSource.Cancel();
            mShutdownSource.Dispose();
        }
    }
}
