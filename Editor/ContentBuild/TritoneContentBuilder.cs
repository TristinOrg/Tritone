using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tritone.Editor.ContentBuild
{
    /// <summary>
    /// Exposes the default active-platform content build through the Tritone menu.
    /// </summary>
    internal static class TritoneContentBuilder
    {
        /// <summary>
        /// Builds assigned AssetBundles for the active platform and opens the output directory.
        /// </summary>
        [MenuItem("Tritone/Build/Content for Active Platform")]
        private static void BuildForActivePlatform()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var version = PlayerSettings.bundleVersion;
            var outputPath = Path.Combine("ContentBuilds", target.ToString(), version);
            var result = ContentBuildPipeline.Build(new ContentBuildOptions(version, outputPath, target));
            Debug.Log($"Tritone content {result.Manifest.Version} built to '{Path.GetFullPath(outputPath)}' with {result.Manifest.Bundles.Count} bundles and {result.Manifest.Assets.Count} assets.");
            EditorUtility.RevealInFinder(result.ManifestPath);
        }
    }
}
