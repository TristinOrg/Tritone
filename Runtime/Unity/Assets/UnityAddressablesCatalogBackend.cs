using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Executes catalog operations through Unity Addressables and releases every operation handle.
    /// </summary>
    public sealed class UnityAddressablesCatalogBackend : IAddressablesCatalogBackend
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<string>> CheckForUpdatesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var handle = Addressables.CheckForCatalogUpdates(false);
            try
            {
                var catalogs = await handle.Task;
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOperation(handle.Status, handle.OperationException, "check Addressables catalogs");
                return CopyStrings(catalogs);
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<string>> UpdateCatalogsAsync(IReadOnlyList<string> catalogIds, CancellationToken cancellationToken)
        {
            if (catalogIds == null)
                throw new ArgumentNullException(nameof(catalogIds));

            cancellationToken.ThrowIfCancellationRequested();
            var handle = Addressables.UpdateCatalogs(catalogIds, false);
            try
            {
                var locators = await handle.Task;
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOperation(handle.Status, handle.OperationException, "update Addressables catalogs");
                var locatorIds = new string[locators.Count];
                for (int i = 0, cnt = locators.Count; i < cnt; i++)
                    locatorIds[i] = locators[i].LocatorId;
                return locatorIds;
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        /// <summary>
        /// Copies Unity-owned catalog results before their operation handle is released.
        /// </summary>
        /// <param name="values">The Unity-owned string list.</param>
        /// <returns>An independently owned immutable result view.</returns>
        private static IReadOnlyList<string> CopyStrings(IList<string> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<string>();

            var result = new string[values.Count];
            for (int i = 0, cnt = values.Count; i < cnt; i++)
                result[i] = values[i];
            return result;
        }

        /// <summary>
        /// Converts an unsuccessful Addressables operation into one stable framework exception.
        /// </summary>
        /// <param name="status">The completed operation status.</param>
        /// <param name="exception">The optional Addressables failure.</param>
        /// <param name="operation">The human-readable operation description.</param>
        private static void ValidateOperation(AsyncOperationStatus status, Exception exception, string operation)
        {
            if (status == AsyncOperationStatus.Succeeded)
                return;

            throw new InvalidOperationException($"Failed to {operation}.", exception);
        }
    }
}
