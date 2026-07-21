using System;
using UnityEditor;

namespace Tritone.Editor.ContentBuild
{
    /// <summary>
    /// Defines one deterministic AssetBundle content build.
    /// </summary>
    public readonly struct ContentBuildOptions
    {
        /// <summary>
        /// Gets the manifest version label.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets the absolute or project-relative output directory.
        /// </summary>
        public string OutputPath { get; }

        /// <summary>
        /// Gets the target Unity player platform.
        /// </summary>
        public BuildTarget Target { get; }

        /// <summary>
        /// Gets the AssetBundle build flags.
        /// </summary>
        public BuildAssetBundleOptions BundleOptions { get; }

        /// <summary>
        /// Gets explicit public address mappings, or an empty array to use asset paths as addresses.
        /// </summary>
        public ContentBuildAsset[] Assets { get; }

        /// <summary>
        /// Initializes one content build definition.
        /// </summary>
        /// <param name="version">The manifest version label.</param>
        /// <param name="outputPath">The absolute or project-relative output directory.</param>
        /// <param name="target">The target Unity player platform.</param>
        /// <param name="bundleOptions">The AssetBundle build flags.</param>
        /// <param name="assets">Explicit public address mappings, or null to use asset paths as addresses.</param>
        public ContentBuildOptions(string version,
                                   string outputPath,
                                   BuildTarget target,
                                   BuildAssetBundleOptions bundleOptions = BuildAssetBundleOptions.ChunkBasedCompression,
                                   ContentBuildAsset[] assets = null)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("A content version is required.", nameof(version));
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("A content output path is required.", nameof(outputPath));

            Version       = version;
            OutputPath    = outputPath;
            Target        = target;
            BundleOptions = bundleOptions;
            Assets        = assets == null ? Array.Empty<ContentBuildAsset>() : (ContentBuildAsset[])assets.Clone();
        }
    }
}
