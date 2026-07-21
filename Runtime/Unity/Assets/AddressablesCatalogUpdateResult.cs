using System.Collections.Generic;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Describes catalogs checked and resource locators activated by one update.
    /// </summary>
    public readonly struct AddressablesCatalogUpdateResult
    {
        /// <summary>
        /// Gets the catalog identifiers reported as changed.
        /// </summary>
        public IReadOnlyList<string> CatalogIds { get; }

        /// <summary>
        /// Gets the resource locator identifiers activated by the update.
        /// </summary>
        public IReadOnlyList<string> LocatorIds { get; }

        /// <summary>
        /// Gets whether at least one remote catalog changed.
        /// </summary>
        public bool Updated => CatalogIds.Count > 0;

        /// <summary>
        /// Initializes one immutable catalog update result.
        /// </summary>
        /// <param name="catalogIds">The changed catalog identifiers.</param>
        /// <param name="locatorIds">The activated resource locator identifiers.</param>
        internal AddressablesCatalogUpdateResult(IReadOnlyList<string> catalogIds, IReadOnlyList<string> locatorIds)
        {
            CatalogIds = catalogIds;
            LocatorIds = locatorIds;
        }
    }
}
