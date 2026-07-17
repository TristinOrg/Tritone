using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Content;
using UnityEngine.Networking;

namespace Tritone.Unity.ContentUpdates
{
    /// <summary>
    /// Retrieves manifests and streams bundle files through UnityWebRequest with bounded retries.
    /// </summary>
    public sealed class HttpContentUpdateSource : IContentUpdateSource
    {
        // Stores remote endpoints, local validation, retry, and timeout settings.
        private readonly ContentUpdateOptions mOptions;

        /// <summary>
        /// Initializes one HTTP-backed content source.
        /// </summary>
        /// <param name="options">The validated content update options.</param>
        public HttpContentUpdateSource(ContentUpdateOptions options)
        {
            mOptions = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public async Task<string> GetManifestAsync(CancellationToken cancellationToken)
        {
            Exception lastException = null;
            for (int attempt = 0, cnt = mOptions.RetryCount + 1; attempt < cnt; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using UnityWebRequest request = UnityWebRequest.Get(mOptions.RemoteManifestUri);
                request.timeout = mOptions.RequestTimeoutSeconds;
                try
                {
                    await WaitForRequestAsync(request, null, cancellationToken);
                    return request.downloadHandler.text;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    lastException = exception;
                }
            }

            throw new InvalidOperationException(
                $"Content manifest request failed after {mOptions.RetryCount + 1} attempts.",
                lastException);
        }

        /// <inheritdoc />
        public async Task DownloadBundleAsync(ContentBundle bundle,
                                              string destinationPath,
                                              Action<long> progress,
                                              CancellationToken cancellationToken)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("A temporary destination path is required.", nameof(destinationPath));

            mOptions.Settings.ResolveBundlePath(bundle.FileName);
            var bundleUri = CreateBundleUri(bundle.FileName);
            Exception lastException = null;
            for (int attempt = 0, cnt = mOptions.RetryCount + 1; attempt < cnt; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                using UnityWebRequest request = new(bundleUri, UnityWebRequest.kHttpVerbGET);
                DownloadHandlerFile handler = new(destinationPath)
                {
                    removeFileOnAbort = true
                };
                request.downloadHandler = handler;
                request.timeout         = mOptions.RequestTimeoutSeconds;
                try
                {
                    await WaitForRequestAsync(request, progress, cancellationToken);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    lastException = exception;
                }
            }

            throw new InvalidOperationException(
                $"Bundle '{bundle.FileName}' download failed after {mOptions.RetryCount + 1} attempts.",
                lastException);
        }

        /// <summary>
        /// Polls one UnityWebRequest without allocating a custom yield instruction.
        /// </summary>
        /// <param name="request">The configured request to send.</param>
        /// <param name="progress">The optional callback receiving downloaded bytes.</param>
        /// <param name="cancellationToken">The token used to abort the request.</param>
        /// <returns>A task completed when the request succeeds.</returns>
        private static async Task WaitForRequestAsync(UnityWebRequest request,
                                                      Action<long> progress,
                                                      CancellationToken cancellationToken)
        {
            var operation = request.SendWebRequest();
            try
            {
                while (!operation.isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ReportDownloadedBytes(request, progress);
                    await Task.Yield();
                }
                cancellationToken.ThrowIfCancellationRequested();
                ReportDownloadedBytes(request, progress);
            }
            catch (OperationCanceledException)
            {
                request.Abort();
                throw;
            }

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(
                    $"HTTP content request failed. Code: {request.responseCode}, Error: {request.error}");
        }

        /// <summary>
        /// Reports Unity's unsigned byte counter through the signed public progress contract.
        /// </summary>
        /// <param name="request">The active request.</param>
        /// <param name="progress">The optional byte callback.</param>
        private static void ReportDownloadedBytes(UnityWebRequest request, Action<long> progress)
        {
            if (progress == null)
                return;

            var downloadedBytes = request.downloadedBytes > (ulong)long.MaxValue
                ? long.MaxValue
                : (long)request.downloadedBytes;
            progress.Invoke(downloadedBytes);
        }

        /// <summary>
        /// Escapes every portable file path segment and appends it beneath the bundle root.
        /// </summary>
        /// <param name="fileName">The validated portable relative bundle file name.</param>
        /// <returns>The complete escaped bundle URI.</returns>
        private Uri CreateBundleUri(string fileName)
        {
            var segments = fileName.Split('/');
            StringBuilder builder = new(fileName.Length + 16);
            for (int i = 0, cnt = segments.Length; i < cnt; i++)
            {
                if (i > 0)
                    builder.Append('/');
                builder.Append(Uri.EscapeDataString(segments[i]));
            }
            return new Uri(mOptions.RemoteBundleRootUri, builder.ToString());
        }
    }
}
