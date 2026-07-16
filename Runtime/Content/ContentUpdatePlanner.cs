using System;
using System.Collections.Generic;

namespace Tritone.Content
{
    /// <summary>
    /// Compares validated manifests and creates the minimal deterministic local file update plan.
    /// </summary>
    public static class ContentUpdatePlanner
    {
        /// <summary>
        /// Creates the file operations required to reach one remote content manifest.
        /// </summary>
        /// <param name="localManifest">The currently installed manifest, or null for a first installation.</param>
        /// <param name="remoteManifest">The validated remote manifest that should become active.</param>
        /// <returns>An immutable deterministic content update plan.</returns>
        public static ContentUpdatePlan CreatePlan(ContentManifest localManifest,
                                                   ContentManifest remoteManifest)
        {
            if (remoteManifest == null)
                throw new ArgumentNullException(nameof(remoteManifest));

            List<ContentBundle> downloads = new(remoteManifest.Bundles.Count);
            List<string> obsoleteFiles    = localManifest == null ? new() : new(localManifest.Bundles.Count);
            long downloadBytes            = 0;

            var remoteBundles = remoteManifest.Bundles;
            for (int i = 0, cnt = remoteBundles.Count; i < cnt; i++)
            {
                var remoteBundle = remoteBundles[i];
                if (HasMatchingLocalFile(localManifest, remoteBundle))
                    continue;

                downloads.Add(remoteBundle);
                downloadBytes = checked(downloadBytes + remoteBundle.Size);
            }

            if (localManifest != null)
            {
                var localBundles = localManifest.Bundles;
                for (int i = 0, cnt = localBundles.Count; i < cnt; i++)
                {
                    var localBundle = localBundles[i];
                    if (!remoteManifest.TryGetBundleByFileName(localBundle.FileName, out _))
                        obsoleteFiles.Add(localBundle.FileName);
                }
            }

            return new ContentUpdatePlan(remoteManifest,
                                         downloads.ToArray(),
                                         obsoleteFiles.ToArray(),
                                         downloadBytes);
        }

        /// <summary>
        /// Checks whether the local manifest already identifies the exact required file content.
        /// </summary>
        /// <param name="localManifest">The currently installed manifest, or null.</param>
        /// <param name="remoteBundle">The required remote bundle definition.</param>
        /// <returns>True when no download is required for the remote bundle.</returns>
        private static bool HasMatchingLocalFile(ContentManifest localManifest,
                                                 ContentBundle remoteBundle)
        {
            if (localManifest == null)
                return false;
            if (!localManifest.TryGetBundleByFileName(remoteBundle.FileName, out var localBundle))
                return false;

            return localBundle.Size == remoteBundle.Size &&
                   string.Equals(localBundle.Hash, remoteBundle.Hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
