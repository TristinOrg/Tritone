using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Checks and activates remote Addressables catalog updates.
    /// </summary>
    public interface IAddressablesCatalogService
    {
        /// <summary>
        /// Checks for changed catalogs and activates their latest resource locators.
        /// </summary>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>A task containing the catalogs and locators changed by the operation.</returns>
        Task<AddressablesCatalogUpdateResult> UpdateCatalogsAsync(CancellationToken cancellationToken = default);
    }
}
