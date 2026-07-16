using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tritone.Content
{
    /// <summary>
    /// Stores one validated immutable snapshot of versioned bundles and addressed assets.
    /// </summary>
    public sealed class ContentManifest
    {
        // Stores bundles in deterministic manifest order.
        private readonly ContentBundle[] mBundles;

        // Stores assets in deterministic manifest order.
        private readonly ContentAsset[] mAssets;

        // Exposes bundles without allowing the private array to be cast and modified.
        private readonly ReadOnlyCollection<ContentBundle> mReadOnlyBundles;

        // Exposes assets without allowing the private array to be cast and modified.
        private readonly ReadOnlyCollection<ContentAsset> mReadOnlyAssets;

        // Stores bundles by stable logical name for allocation-free lookup.
        private readonly Dictionary<string, ContentBundle> mBundlesByName;

        // Stores bundles by platform-specific file name for cache comparison.
        private readonly Dictionary<string, ContentBundle> mBundlesByFileName;

        // Gets the manifest version label.
        public string Version { get; }

        // Gets the validated bundle definitions in deterministic order.
        public IReadOnlyList<ContentBundle> Bundles => mReadOnlyBundles;

        // Gets the validated asset definitions in deterministic order.
        public IReadOnlyList<ContentAsset> Assets => mReadOnlyAssets;

        /// <summary>
        /// Initializes and validates one immutable content manifest.
        /// </summary>
        /// <param name="version">The manifest version label.</param>
        /// <param name="bundles">The complete bundle definitions for this version.</param>
        /// <param name="assets">The complete addressed asset definitions for this version.</param>
        public ContentManifest(string version,
                               ContentBundle[] bundles,
                               ContentAsset[] assets)
        {
            ContentValidation.ValidateValue(version, nameof(version));

            Version            = version;
            mBundles           = bundles == null || bundles.Length == 0 ? Array.Empty<ContentBundle>() : (ContentBundle[])bundles.Clone();
            mAssets            = assets == null || assets.Length == 0 ? Array.Empty<ContentAsset>() : (ContentAsset[])assets.Clone();
            mReadOnlyBundles   = Array.AsReadOnly(mBundles);
            mReadOnlyAssets    = Array.AsReadOnly(mAssets);
            mBundlesByName     = new(mBundles.Length, StringComparer.Ordinal);
            mBundlesByFileName = new(mBundles.Length, StringComparer.Ordinal);

            ValidateBundles();
            ValidateAssets();
        }

        /// <summary>
        /// Tries to find one bundle by its stable logical name.
        /// </summary>
        /// <param name="bundleName">The stable logical bundle name.</param>
        /// <param name="bundle">The matching bundle when found.</param>
        /// <returns>True when the manifest contains the logical bundle name.</returns>
        public bool TryGetBundle(string bundleName, out ContentBundle bundle)
        {
            if (bundleName != null)
                return mBundlesByName.TryGetValue(bundleName, out bundle);

            bundle = null;
            return false;
        }

        /// <summary>
        /// Tries to find one bundle by its platform-specific file name.
        /// </summary>
        /// <param name="fileName">The platform-specific bundle file name.</param>
        /// <param name="bundle">The matching bundle when found.</param>
        /// <returns>True when the manifest contains the bundle file name.</returns>
        public bool TryGetBundleByFileName(string fileName, out ContentBundle bundle)
        {
            if (fileName != null)
                return mBundlesByFileName.TryGetValue(fileName, out bundle);

            bundle = null;
            return false;
        }

        /// <summary>
        /// Validates bundle identities, file identities, dependency references, and dependency cycles.
        /// </summary>
        private void ValidateBundles()
        {
            for (int i = 0, cnt = mBundles.Length; i < cnt; i++)
            {
                var bundle = mBundles[i] ?? throw new InvalidOperationException($"Content bundle at index {i} is null.");
                if (!mBundlesByName.TryAdd(bundle.Name, bundle))
                    throw new InvalidOperationException($"Content bundle '{bundle.Name}' is duplicated.");
                if (!mBundlesByFileName.TryAdd(bundle.FileName, bundle))
                    throw new InvalidOperationException($"Content bundle file '{bundle.FileName}' is used more than once.");
            }

            Dictionary<string, byte> states = new(mBundles.Length, StringComparer.Ordinal);
            List<string> path                = new(mBundles.Length);
            for (int i = 0, cnt = mBundles.Length; i < cnt; i++)
                ValidateDependencyPath(mBundles[i], states, path);
        }

        /// <summary>
        /// Validates one dependency path and detects cycles in linear graph time.
        /// </summary>
        /// <param name="bundle">The bundle currently being visited.</param>
        /// <param name="states">The visitation state for every reached bundle.</param>
        /// <param name="path">The active dependency path used for diagnostics.</param>
        private void ValidateDependencyPath(ContentBundle bundle,
                                            Dictionary<string, byte> states,
                                            List<string> path)
        {
            if (states.TryGetValue(bundle.Name, out var state))
            {
                if (state == 2)
                    return;
                if (state == 1)
                {
                    path.Add(bundle.Name);
                    throw new InvalidOperationException($"Content bundle dependency cycle detected: {string.Join(" -> ", path)}.");
                }
            }

            states[bundle.Name] = 1;
            path.Add(bundle.Name);
            var dependencies = bundle.Dependencies;
            for (int i = 0, cnt = dependencies.Count; i < cnt; i++)
            {
                var dependencyName = dependencies[i];
                if (!mBundlesByName.TryGetValue(dependencyName, out var dependency))
                    throw new InvalidOperationException($"Content bundle '{bundle.Name}' references missing dependency '{dependencyName}'.");
                ValidateDependencyPath(dependency, states, path);
            }
            path.RemoveAt(path.Count - 1);
            states[bundle.Name] = 2;
        }

        /// <summary>
        /// Validates unique asset addresses and containing bundle references.
        /// </summary>
        private void ValidateAssets()
        {
            HashSet<string> addresses = new(StringComparer.Ordinal);
            for (int i = 0, cnt = mAssets.Length; i < cnt; i++)
            {
                var asset = mAssets[i] ?? throw new InvalidOperationException($"Content asset at index {i} is null.");
                if (!addresses.Add(asset.Address))
                    throw new InvalidOperationException($"Content asset address '{asset.Address}' is duplicated.");
                if (!mBundlesByName.ContainsKey(asset.BundleName))
                    throw new InvalidOperationException($"Content asset '{asset.Address}' references missing bundle '{asset.BundleName}'.");
            }
        }
    }

    /// <summary>
    /// Provides shared value validation for immutable content definitions.
    /// </summary>
    internal static class ContentValidation
    {
        /// <summary>
        /// Rejects missing content identifiers and metadata values.
        /// </summary>
        /// <param name="value">The value that must contain non-whitespace text.</param>
        /// <param name="parameterName">The public parameter name reported on failure.</param>
        internal static void ValidateValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Content values cannot be null, empty, or whitespace.", parameterName);
        }
    }
}
