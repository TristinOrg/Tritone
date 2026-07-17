namespace Tritone.Content
{
    /// <summary>
    /// Reports allocation-free progress values for one content update operation.
    /// </summary>
    public readonly struct ContentUpdateProgress
    {
        // Gets the current update stage.
        public EContentUpdateStage Stage { get; }

        // Gets the current bundle file name, or null outside bundle-specific stages.
        public string FileName { get; }

        // Gets the number of download bytes completed across the complete plan.
        public long CompletedBytes { get; }

        // Gets the total number of download bytes in the complete plan.
        public long TotalBytes { get; }

        // Gets the number of bundle downloads already verified.
        public int CompletedFiles { get; }

        // Gets the total number of bundle downloads in the complete plan.
        public int TotalFiles { get; }

        // Gets normalized byte progress from zero through one.
        public double NormalizedProgress => TotalBytes > 0
            ? (double)CompletedBytes / TotalBytes
            : Stage == EContentUpdateStage.Completed ? 1.0 : 0.0;

        /// <summary>
        /// Initializes one immutable content update progress snapshot.
        /// </summary>
        /// <param name="stage">The current update stage.</param>
        /// <param name="fileName">The current bundle file name, or null.</param>
        /// <param name="completedBytes">The completed download bytes across the plan.</param>
        /// <param name="totalBytes">The total download bytes across the plan.</param>
        /// <param name="completedFiles">The number of verified downloads.</param>
        /// <param name="totalFiles">The total number of downloads.</param>
        internal ContentUpdateProgress(EContentUpdateStage stage,
                                       string fileName,
                                       long completedBytes,
                                       long totalBytes,
                                       int completedFiles,
                                       int totalFiles)
        {
            Stage          = stage;
            FileName       = fileName;
            CompletedBytes = completedBytes;
            TotalBytes     = totalBytes;
            CompletedFiles = completedFiles;
            TotalFiles     = totalFiles;
        }
    }
}
