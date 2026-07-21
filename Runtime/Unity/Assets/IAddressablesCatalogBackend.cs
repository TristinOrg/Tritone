using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Abstracts Addressables catalog operations for deterministic integration testing.
    /// </summary>
    public interface IAddressablesCatalogBackend
    {
        /// <summary>
        /// Gets catalog identifiers whose remote hashes have changed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token checked around the Unity operation.</param>
        /// <returns>A task containing catalog identifiers requiring an update.</returns>
        Task<IReadOnlyList<string>> CheckForUpdatesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Updates changed catalogs and activates their latest resource locators.
        /// </summary>
        /// <param name="catalogIds">The catalog identifiers returned by the preceding check.</param>
        /// <param name="cancellationToken">The cancellation token checked around the Unity operation.</param>
        /// <returns>A task containing identifiers of the activated resource locators.</returns>
        Task<IReadOnlyList<string>> UpdateCatalogsAsync(IReadOnlyList<string> catalogIds, CancellationToken cancellationToken);
    }
}
