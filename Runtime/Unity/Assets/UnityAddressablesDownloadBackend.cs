using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Executes dependency cache operations through Unity Addressables.
    /// </summary>
    public sealed class UnityAddressablesDownloadBackend : IAddressablesDownloadBackend
    {
        /// <inheritdoc />
        public async Task<long> GetDownloadSizeAsync(string key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var handle = Addressables.GetDownloadSizeAsync(key);
            try
            {
                var size = await handle.Task;
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOperation(handle.Status, handle.OperationException, "get Addressables download size");
                return size;
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        /// <inheritdoc />
        public async Task DownloadDependenciesAsync(string key, Action<AddressablesDownloadProgress> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var handle = Addressables.DownloadDependenciesAsync(key, false);
            try
            {
                while (!handle.IsDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ReportProgress(handle, progress);
                    await Task.Yield();
                }

                await handle.Task;
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOperation(handle.Status, handle.OperationException, "download Addressables dependencies");
                ReportProgress(handle, progress);
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        /// <summary>
        /// Reports one operation download snapshot when a listener exists.
        /// </summary>
        /// <param name="handle">The active dependency operation.</param>
        /// <param name="progress">The optional progress callback.</param>
        private static void ReportProgress(AsyncOperationHandle handle, Action<AddressablesDownloadProgress> progress)
        {
            if (progress == null)
                return;

            var status = handle.GetDownloadStatus();
            progress.Invoke(new AddressablesDownloadProgress(status.DownloadedBytes, status.TotalBytes));
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
