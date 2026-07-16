using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tritone.Content
{
    /// <summary>
    /// Describes the deterministic downloads and removals required to reach one remote manifest.
    /// </summary>
    public sealed class ContentUpdatePlan
    {
        // Stores bundles that must be downloaded in remote manifest order.
        private readonly ContentBundle[] mDownloads;

        // Stores local files that are no longer referenced by the remote manifest.
        private readonly string[] mObsoleteFiles;

        // Exposes downloads without allowing the private array to be cast and modified.
        private readonly ReadOnlyCollection<ContentBundle> mReadOnlyDownloads;

        // Exposes obsolete files without allowing the private array to be cast and modified.
        private readonly ReadOnlyCollection<string> mReadOnlyObsoleteFiles;

        // Gets the remote manifest that becomes active after a successful update.
        public ContentManifest TargetManifest { get; }

        // Gets bundles that must be downloaded in deterministic order.
        public IReadOnlyList<ContentBundle> Downloads => mReadOnlyDownloads;

        // Gets local bundle files that may be removed after all downloads succeed.
        public IReadOnlyList<string> ObsoleteFiles => mReadOnlyObsoleteFiles;

        // Gets the total number of bytes expected across required downloads.
        public long DownloadBytes { get; }

        // Gets whether any local file operation is required.
        public bool HasChanges => mDownloads.Length > 0 || mObsoleteFiles.Length > 0;

        /// <summary>
        /// Initializes one immutable content update plan.
        /// </summary>
        /// <param name="targetManifest">The remote manifest that the update reaches.</param>
        /// <param name="downloads">The bundles that must be downloaded.</param>
        /// <param name="obsoleteFiles">The local files that become obsolete.</param>
        /// <param name="downloadBytes">The total expected download size in bytes.</param>
        internal ContentUpdatePlan(ContentManifest targetManifest,
                                   ContentBundle[] downloads,
                                   string[] obsoleteFiles,
                                   long downloadBytes)
        {
            TargetManifest         = targetManifest;
            mDownloads             = downloads;
            mObsoleteFiles         = obsoleteFiles;
            mReadOnlyDownloads     = Array.AsReadOnly(mDownloads);
            mReadOnlyObsoleteFiles = Array.AsReadOnly(mObsoleteFiles);
            DownloadBytes          = downloadBytes;
        }
    }
}
