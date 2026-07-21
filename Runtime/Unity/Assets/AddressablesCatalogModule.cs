using System;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Kernel;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Serializes Addressables catalog updates and binds them to application shutdown.
    /// </summary>
    public sealed class AddressablesCatalogModule : ModuleBase, IAddressablesCatalogService
    {
        /// <summary>
        /// Stores an immutable empty result list.
        /// </summary>
        private static readonly string[] sEmptyIds = Array.Empty<string>();

        /// <summary>
        /// Executes concrete Addressables catalog operations.
        /// </summary>
        private readonly IAddressablesCatalogBackend mBackend;

        /// <summary>
        /// Serializes catalog checks and locator replacement.
        /// </summary>
        private readonly SemaphoreSlim mUpdateLock = new(1, 1);

        /// <summary>
        /// Cancels active catalog operations when the application stops.
        /// </summary>
        private readonly CancellationTokenSource mShutdownSource = new();

        /// <summary>
        /// Indicates whether application shutdown has begun.
        /// </summary>
        private bool mStopped;

        /// <summary>
        /// Initializes catalog updates with one explicit backend.
        /// </summary>
        /// <param name="backend">The backend executing Addressables catalog operations.</param>
        public AddressablesCatalogModule(IAddressablesCatalogBackend backend)
        {
            mBackend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        /// <summary>
        /// Registers application-wide Addressables catalog update access.
        /// </summary>
        /// <param name="services">The application service registry.</param>
        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton<IAddressablesCatalogService>(this);
        }

        /// <inheritdoc />
        public async Task<AddressablesCatalogUpdateResult> UpdateCatalogsAsync(CancellationToken cancellationToken = default)
        {
            if (mStopped)
                throw new ObjectDisposedException(nameof(AddressablesCatalogModule));

            using var updateSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, mShutdownSource.Token);
            await mUpdateLock.WaitAsync(updateSource.Token);
            try
            {
                var catalogIds = await mBackend.CheckForUpdatesAsync(updateSource.Token);
                if (catalogIds == null)
                    throw new InvalidOperationException("The Addressables catalog backend returned a null check result.");
                if (catalogIds.Count == 0)
                    return new AddressablesCatalogUpdateResult(sEmptyIds, sEmptyIds);

                var locatorIds = await mBackend.UpdateCatalogsAsync(catalogIds, updateSource.Token);
                if (locatorIds == null)
                    throw new InvalidOperationException("The Addressables catalog backend returned a null update result.");
                return new AddressablesCatalogUpdateResult(catalogIds, locatorIds);
            }
            finally
            {
                mUpdateLock.Release();
            }
        }

        /// <summary>
        /// Cancels active operations and rejects future catalog updates.
        /// </summary>
        protected override void OnStop()
        {
            mStopped = true;
            mShutdownSource.Cancel();
            mShutdownSource.Dispose();
        }
    }
}
