using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Unity.Assets;

namespace Tritone.Tests
{
    /// <summary>
    /// Provides deterministic catalog results without accessing remote Addressables content.
    /// </summary>
    internal sealed class FakeAddressablesCatalogBackend : IAddressablesCatalogBackend
    {
        /// <summary>
        /// Stores catalog identifiers returned by checks.
        /// </summary>
        private readonly IReadOnlyList<string> mCatalogIds;

        /// <summary>
        /// Stores locator identifiers returned by updates.
        /// </summary>
        private readonly IReadOnlyList<string> mLocatorIds;

        /// <summary>
        /// Gets the number of catalog checks.
        /// </summary>
        internal int CheckCount { get; private set; }

        /// <summary>
        /// Gets the number of catalog updates.
        /// </summary>
        internal int UpdateCount { get; private set; }

        /// <summary>
        /// Gets the catalog identifiers supplied to the latest update.
        /// </summary>
        internal IReadOnlyList<string> LastUpdatedCatalogIds { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// Initializes one backend with deterministic check and update results.
        /// </summary>
        /// <param name="catalogIds">The catalog identifiers returned by checks.</param>
        /// <param name="locatorIds">The locator identifiers returned by updates.</param>
        internal FakeAddressablesCatalogBackend(IReadOnlyList<string> catalogIds, IReadOnlyList<string> locatorIds)
        {
            mCatalogIds = catalogIds;
            mLocatorIds = locatorIds;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<string>> CheckForUpdatesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckCount++;
            return Task.FromResult(mCatalogIds);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<string>> UpdateCatalogsAsync(IReadOnlyList<string> catalogIds, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            UpdateCount++;
            LastUpdatedCatalogIds = catalogIds;
            return Task.FromResult(mLocatorIds);
        }
    }
}
