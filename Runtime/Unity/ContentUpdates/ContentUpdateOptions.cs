using System;
using Tritone.Content;

namespace Tritone.Unity.ContentUpdates
{
    /// <summary>
    /// Defines remote endpoints, local storage, and retry behavior for content updates.
    /// </summary>
    public sealed class ContentUpdateOptions
    {
        // Gets the validated remote manifest URI.
        public Uri RemoteManifestUri { get; }

        // Gets the validated remote bundle root URI ending in a slash.
        public Uri RemoteBundleRootUri { get; }

        // Gets the validated local storage settings.
        public ContentUpdateSettings Settings { get; }

        // Gets the number of retries after the first failed HTTP attempt.
        public int RetryCount { get; }

        // Gets the timeout in seconds for each HTTP attempt.
        public int RequestTimeoutSeconds { get; }

        /// <summary>
        /// Initializes one complete content update configuration.
        /// </summary>
        /// <param name="remoteManifestUrl">The absolute URL of the remote manifest.</param>
        /// <param name="remoteBundleRootUrl">The absolute root URL containing remote bundle files.</param>
        /// <param name="localRootPath">The absolute local directory containing active content.</param>
        /// <param name="manifestFileName">The relative local manifest file name.</param>
        /// <param name="retryCount">The number of retries after the first failed request.</param>
        /// <param name="requestTimeoutSeconds">The timeout in seconds for each request attempt.</param>
        public ContentUpdateOptions(string remoteManifestUrl,
                                    string remoteBundleRootUrl,
                                    string localRootPath,
                                    string manifestFileName   = "content-manifest.json",
                                    int retryCount            = 2,
                                    int requestTimeoutSeconds = 30)
        {
            RemoteManifestUri   = CreateAbsoluteUri(remoteManifestUrl, nameof(remoteManifestUrl));
            RemoteBundleRootUri = EnsureTrailingSlash(
                CreateAbsoluteUri(remoteBundleRootUrl, nameof(remoteBundleRootUrl)));
            Settings            = new ContentUpdateSettings(localRootPath, manifestFileName);

            if (retryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount));
            if (requestTimeoutSeconds < 1)
                throw new ArgumentOutOfRangeException(nameof(requestTimeoutSeconds));

            RetryCount            = retryCount;
            RequestTimeoutSeconds = requestTimeoutSeconds;
        }

        /// <summary>
        /// Creates and validates one absolute remote URI.
        /// </summary>
        /// <param name="url">The configured absolute URL.</param>
        /// <param name="parameterName">The public parameter name used by validation errors.</param>
        /// <returns>The validated absolute URI.</returns>
        private static Uri CreateAbsoluteUri(string url, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(url) ||
                !Uri.TryCreate(url, UriKind.Absolute, out var uri))
                throw new ArgumentException("An absolute content URL is required.", parameterName);
            return uri;
        }

        /// <summary>
        /// Ensures that relative bundle paths append beneath the configured root.
        /// </summary>
        /// <param name="uri">The validated absolute bundle root URI.</param>
        /// <returns>An equivalent URI ending in a slash.</returns>
        private static Uri EnsureTrailingSlash(Uri uri)
        {
            var value = uri.AbsoluteUri;
            return value.EndsWith("/", StringComparison.Ordinal)
                ? uri
                : new Uri(value + "/", UriKind.Absolute);
        }
    }
}
