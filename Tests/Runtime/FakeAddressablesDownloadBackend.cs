using System;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Unity.Assets;

namespace Tritone.Tests
{
    /// <summary>
    /// Provides deterministic dependency download behavior for tests.
    /// </summary>
    internal sealed class FakeAddressablesDownloadBackend : IAddressablesDownloadBackend
    {
        /// <summary>
        /// Stores the uncached byte count returned by size checks.
        /// </summary>
        private readonly long mDownloadBytes;

        /// <summary>
        /// Gets the number of size checks.
        /// </summary>
        internal int SizeCheckCount { get; private set; }

        /// <summary>
        /// Gets the number of dependency downloads.
        /// </summary>
        internal int DownloadCount { get; private set; }

        /// <summary>
        /// Gets the number of dependency cache clears.
        /// </summary>
        internal int ClearCount { get; private set; }

        /// <summary>
        /// Initializes one backend with a deterministic uncached byte count.
        /// </summary>
        /// <param name="downloadBytes">The byte count returned by size checks.</param>
        internal FakeAddressablesDownloadBackend(long downloadBytes)
        {
            mDownloadBytes = downloadBytes;
        }

        /// <inheritdoc />
        public Task<long> GetDownloadSizeAsync(string key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SizeCheckCount++;
            return Task.FromResult(mDownloadBytes);
        }

        /// <inheritdoc />
        public Task DownloadDependenciesAsync(string key, Action<AddressablesDownloadProgress> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DownloadCount++;
            progress?.Invoke(new AddressablesDownloadProgress(mDownloadBytes, mDownloadBytes));
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<bool> ClearDependencyCacheAsync(string key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ClearCount++;
            return Task.FromResult(true);
        }
    }
}
