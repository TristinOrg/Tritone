namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Reports an allocation-free Addressables dependency download snapshot.
    /// </summary>
    public readonly struct AddressablesDownloadProgress
    {
        /// <summary>
        /// Gets downloaded bytes reported by Addressables.
        /// </summary>
        public long DownloadedBytes { get; }

        /// <summary>
        /// Gets total bytes reported by Addressables.
        /// </summary>
        public long TotalBytes { get; }

        /// <summary>
        /// Gets normalized progress from zero through one.
        /// </summary>
        public float NormalizedProgress => TotalBytes > 0 ? (float)DownloadedBytes / TotalBytes : 1.0f;

        /// <summary>
        /// Initializes one immutable download progress snapshot.
        /// </summary>
        /// <param name="downloadedBytes">The downloaded byte count.</param>
        /// <param name="totalBytes">The total byte count.</param>
        public AddressablesDownloadProgress(long downloadedBytes, long totalBytes)
        {
            DownloadedBytes = downloadedBytes;
            TotalBytes      = totalBytes;
        }
    }
}
