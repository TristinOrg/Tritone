namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Describes one completed Addressables dependency preload.
    /// </summary>
    public readonly struct AddressablesDownloadResult
    {
        /// <summary>
        /// Gets the requested address or label.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the uncached byte count measured before downloading.
        /// </summary>
        public long DownloadedBytes { get; }

        /// <summary>
        /// Gets whether remote dependencies required a download.
        /// </summary>
        public bool Downloaded => DownloadedBytes > 0;

        /// <summary>
        /// Initializes one immutable download result.
        /// </summary>
        /// <param name="key">The requested address or label.</param>
        /// <param name="downloadedBytes">The uncached byte count measured before downloading.</param>
        internal AddressablesDownloadResult(string key, long downloadedBytes)
        {
            Key             = key;
            DownloadedBytes = downloadedBytes;
        }
    }
}
