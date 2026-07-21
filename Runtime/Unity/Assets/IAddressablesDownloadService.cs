using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Preloads remote Addressables dependencies before gameplay needs them.
    /// </summary>
    public interface IAddressablesDownloadService
    {
        /// <summary>
        /// Downloads uncached dependencies for one Addressables key.
        /// </summary>
        /// <param name="key">The address or label whose dependencies must be cached.</param>
        /// <param name="progress">The optional allocation-free progress callback.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>A task containing the completed download summary.</returns>
        Task<AddressablesDownloadResult> DownloadAsync(string key, Action<AddressablesDownloadProgress> progress = null, CancellationToken cancellationToken = default);
    }
}
