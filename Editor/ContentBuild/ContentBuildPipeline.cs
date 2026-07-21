using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Tritone.Content;
using Tritone.Unity.ContentUpdates;
using UnityEditor;
using UnityEngine;

namespace Tritone.Editor.ContentBuild
{
    /// <summary>
    /// Builds assigned AssetBundles and emits a runtime-compatible versioned content manifest.
    /// </summary>
    public static class ContentBuildPipeline
    {
        /// <summary>
        /// Stores the generated content manifest file name.
        /// </summary>
        public const string ManifestFileName = "manifest.json";

        /// <summary>
        /// Builds all assigned AssetBundles and writes their deterministic content manifest.
        /// </summary>
        /// <param name="options">The complete content build definition.</param>
        /// <returns>The generated manifest and output location.</returns>
        public static ContentBuildResult Build(ContentBuildOptions options)
        {
            var outputPath = Path.GetFullPath(options.OutputPath);
            Directory.CreateDirectory(outputPath);
            var unityManifest = BuildPipeline.BuildAssetBundles(outputPath, options.BundleOptions, options.Target);
            if (!unityManifest)
                throw new InvalidOperationException("Unity did not produce an AssetBundle manifest.");

            var manifest = CreateManifest(options.Version, outputPath, unityManifest, options.Assets);
            var manifestPath = Path.Combine(outputPath, ManifestFileName);
            var serializer = new UnityJsonContentManifestSerializer();
            WriteTextAtomically(manifestPath, serializer.Serialize(manifest));
            return new ContentBuildResult(manifest, manifestPath);
        }

        /// <summary>
        /// Creates a runtime content manifest from one completed Unity AssetBundle build.
        /// </summary>
        /// <param name="version">The manifest version label.</param>
        /// <param name="outputPath">The complete AssetBundle output directory.</param>
        /// <param name="unityManifest">Unity's completed AssetBundle dependency manifest.</param>
        /// <param name="configuredAssets">Explicit public address mappings, or an empty array to use asset paths.</param>
        /// <returns>A validated runtime content manifest.</returns>
        internal static ContentManifest CreateManifest(string version,
                                                       string outputPath,
                                                       AssetBundleManifest unityManifest,
                                                       ContentBuildAsset[] configuredAssets)
        {
            var bundleNames = unityManifest.GetAllAssetBundles();
            Array.Sort(bundleNames, StringComparer.Ordinal);
            var bundles = new ContentBundle[bundleNames.Length];
            var bundleByAssetPath = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < bundleNames.Length; i++)
            {
                var bundleName = bundleNames[i];
                var filePath = Path.Combine(outputPath, bundleName);
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"Built AssetBundle '{bundleName}' is missing.", filePath);

                var dependencies = unityManifest.GetDirectDependencies(bundleName);
                Array.Sort(dependencies, StringComparer.Ordinal);
                bundles[i] = new ContentBundle(bundleName, bundleName, ComputeSha256(filePath), new FileInfo(filePath).Length, dependencies);

                var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
                Array.Sort(assetPaths, StringComparer.Ordinal);
                foreach (var assetPath in assetPaths)
                {
                    if (!bundleByAssetPath.TryAdd(assetPath, bundleName))
                        throw new InvalidOperationException($"Asset '{assetPath}' is included in multiple AssetBundles.");
                }
            }

            var assets = CreateAssets(bundleByAssetPath, configuredAssets);
            return new ContentManifest(version, bundles, assets);
        }

        /// <summary>
        /// Creates deterministic addressed asset entries from bundle assignments and optional mappings.
        /// </summary>
        /// <param name="bundleByAssetPath">The built bundle name for every included asset path.</param>
        /// <param name="configuredAssets">Explicit public address mappings, or an empty array to use asset paths.</param>
        /// <returns>Sorted runtime content asset entries.</returns>
        internal static ContentAsset[] CreateAssets(Dictionary<string, string> bundleByAssetPath, ContentBuildAsset[] configuredAssets)
        {
            var assets = new ContentAsset[configuredAssets.Length == 0 ? bundleByAssetPath.Count : configuredAssets.Length];
            if (configuredAssets.Length == 0)
            {
                var assetPaths = new string[bundleByAssetPath.Count];
                bundleByAssetPath.Keys.CopyTo(assetPaths, 0);
                Array.Sort(assetPaths, StringComparer.Ordinal);
                for (var i = 0; i < assetPaths.Length; i++)
                {
                    var assetPath = assetPaths[i];
                    assets[i] = new ContentAsset(assetPath, bundleByAssetPath[assetPath], assetPath);
                }
                return assets;
            }

            var sortedAssets = (ContentBuildAsset[])configuredAssets.Clone();
            Array.Sort(sortedAssets, CompareAssets);
            for (var i = 0; i < sortedAssets.Length; i++)
            {
                var asset = sortedAssets[i];
                if (!bundleByAssetPath.TryGetValue(asset.AssetPath, out var bundleName))
                    throw new InvalidOperationException($"Content address '{asset.Address}' references asset '{asset.AssetPath}' that is not included in a built AssetBundle.");
                assets[i] = new ContentAsset(asset.Address, bundleName, asset.AssetPath);
            }
            return assets;
        }

        /// <summary>
        /// Compares public asset mappings by address and then by asset path.
        /// </summary>
        /// <param name="left">The left mapping.</param>
        /// <param name="right">The right mapping.</param>
        /// <returns>A signed ordering value.</returns>
        private static int CompareAssets(ContentBuildAsset left, ContentBuildAsset right)
        {
            var result = string.Compare(left.Address, right.Address, StringComparison.Ordinal);
            return result != 0 ? result : string.Compare(left.AssetPath, right.AssetPath, StringComparison.Ordinal);
        }

        /// <summary>
        /// Computes a lowercase SHA-256 hash without loading a complete bundle into memory.
        /// </summary>
        /// <param name="filePath">The complete bundle file path.</param>
        /// <returns>The lowercase hexadecimal SHA-256 hash.</returns>
        private static string ComputeSha256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var algorithm = SHA256.Create();
            var hash = algorithm.ComputeHash(stream);
            var text = new StringBuilder(hash.Length * 2);
            foreach (var value in hash)
                text.Append(value.ToString("x2"));
            return text.ToString();
        }

        /// <summary>
        /// Replaces one UTF-8 text output after its complete contents are available.
        /// </summary>
        /// <param name="path">The complete target file path.</param>
        /// <param name="content">The complete text content.</param>
        private static void WriteTextAtomically(string path, string content)
        {
            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, content, new UTF8Encoding(false));
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temporaryPath, path);
        }
    }
}
