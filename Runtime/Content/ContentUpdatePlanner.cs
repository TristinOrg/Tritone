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
        /// <param name="canReuseLocalFile">An optional physical file availability check.</param>
        /// <returns>An immutable deterministic content update plan.</returns>
        public static ContentUpdatePlan CreatePlan(ContentManifest localManifest,
                                                   ContentManifest remoteManifest,
                                                   Func<ContentBundle, bool> canReuseLocalFile = null)
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
                if (HasMatchingLocalFile(localManifest, remoteBundle) &&
                    (canReuseLocalFile == null || canReuseLocalFile.Invoke(remoteBundle)))
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
                                         downloadBytes,
                                         !AreEquivalent(localManifest, remoteManifest));
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

        /// <summary>
        /// Compares complete immutable manifests without serialization or temporary allocations.
        /// </summary>
        /// <param name="localManifest">The currently installed manifest, or null.</param>
        /// <param name="remoteManifest">The required remote manifest.</param>
        /// <returns>True when both manifests describe the same active content state.</returns>
        private static bool AreEquivalent(ContentManifest localManifest,
                                          ContentManifest remoteManifest)
        {
            if (localManifest == null ||
                !string.Equals(localManifest.Version, remoteManifest.Version, StringComparison.Ordinal) ||
                localManifest.Bundles.Count != remoteManifest.Bundles.Count ||
                localManifest.Assets.Count != remoteManifest.Assets.Count)
                return false;

            var localBundles  = localManifest.Bundles;
            var remoteBundles = remoteManifest.Bundles;
            for (int i = 0, cnt = localBundles.Count; i < cnt; i++)
            {
                var localBundle  = localBundles[i];
                var remoteBundle = remoteBundles[i];
                if (!AreEquivalent(localBundle, remoteBundle))
                    return false;
            }

            var localAssets  = localManifest.Assets;
            var remoteAssets = remoteManifest.Assets;
            for (int i = 0, cnt = localAssets.Count; i < cnt; i++)
            {
                var localAsset  = localAssets[i];
                var remoteAsset = remoteAssets[i];
                if (!string.Equals(localAsset.Address, remoteAsset.Address, StringComparison.Ordinal) ||
                    !string.Equals(localAsset.BundleName, remoteAsset.BundleName, StringComparison.Ordinal) ||
                    !string.Equals(localAsset.AssetName, remoteAsset.AssetName, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Compares complete bundle metadata including ordered direct dependencies.
        /// </summary>
        /// <param name="localBundle">The installed bundle definition.</param>
        /// <param name="remoteBundle">The remote bundle definition.</param>
        /// <returns>True when both definitions are equivalent.</returns>
        private static bool AreEquivalent(ContentBundle localBundle,
                                          ContentBundle remoteBundle)
        {
            if (!string.Equals(localBundle.Name, remoteBundle.Name, StringComparison.Ordinal) ||
                !string.Equals(localBundle.FileName, remoteBundle.FileName, StringComparison.Ordinal) ||
                !string.Equals(localBundle.Hash, remoteBundle.Hash, StringComparison.OrdinalIgnoreCase) ||
                localBundle.Size != remoteBundle.Size ||
                localBundle.Dependencies.Count != remoteBundle.Dependencies.Count)
                return false;

            var localDependencies  = localBundle.Dependencies;
            var remoteDependencies = remoteBundle.Dependencies;
            for (int i = 0, cnt = localDependencies.Count; i < cnt; i++)
            {
                if (!string.Equals(localDependencies[i], remoteDependencies[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }
    }
}
