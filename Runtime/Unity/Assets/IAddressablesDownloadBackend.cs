using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Abstracts Addressables dependency downloads for deterministic testing.
    /// </summary>
    public interface IAddressablesDownloadBackend
    {
        /// <summary>
        /// Gets uncached dependency bytes for one Addressables key.
        /// </summary>
        /// <param name="key">The address or label to inspect.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task containing the remaining byte count.</returns>
        Task<long> GetDownloadSizeAsync(string key, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads every uncached dependency for one Addressables key.
        /// </summary>
        /// <param name="key">The address or label to download.</param>
        /// <param name="progress">The optional progress callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task completed after dependencies enter the Addressables cache.</returns>
        Task DownloadDependenciesAsync(string key, Action<AddressablesDownloadProgress> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Removes cached dependencies for one Addressables key.
        /// </summary>
        /// <param name="key">The address or label whose cache must be cleared.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task containing true when matching cache entries were cleared.</returns>
        Task<bool> ClearDependencyCacheAsync(string key, CancellationToken cancellationToken);
    }
}
