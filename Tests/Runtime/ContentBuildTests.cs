using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Tritone.Content;
using Tritone.Editor.ContentBuild;
using Tritone.Unity.ContentUpdates;
using UnityEditor;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies deterministic editor content build configuration and asset mapping.
    /// </summary>
    public sealed class ContentBuildTests
    {
        /// <summary>
        /// Defines the temporary project asset folder used by the build integration test.
        /// </summary>
        private const string TestAssetFolder = "Assets/TritoneContentBuildTests";

        /// <summary>
        /// Defines the temporary project asset path used by the build integration test.
        /// </summary>
        private const string TestAssetPath = TestAssetFolder + "/payload.txt";

        /// <summary>
        /// Verifies that explicit public addresses are sorted and resolved to their assigned bundles.
        /// </summary>
        [Test]
        public void CreateAssets_WithExplicitAddressesReturnsDeterministicMappings()
        {
            var bundles = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Assets/UI/Login.prefab", "ui" },
                { "Assets/Core/Config.asset", "core" }
            };
            var configuredAssets = new[]
            {
                new ContentBuildAsset("UI/Login", "Assets/UI/Login.prefab"),
                new ContentBuildAsset("Config/Core", "Assets/Core/Config.asset")
            };

            var assets = ContentBuildPipeline.CreateAssets(bundles, configuredAssets);

            Assert.AreEqual(2, assets.Length);
            Assert.AreEqual("Config/Core", assets[0].Address);
            Assert.AreEqual("core", assets[0].BundleName);
            Assert.AreEqual("UI/Login", assets[1].Address);
            Assert.AreEqual("ui", assets[1].BundleName);
        }

        /// <summary>
        /// Verifies that missing AssetBundle assignments stop manifest generation.
        /// </summary>
        [Test]
        public void CreateAssets_WithUnassignedAssetThrows()
        {
            var configuredAssets = new[]
            {
                new ContentBuildAsset("UI/Login", "Assets/UI/Login.prefab")
            };

            try
            {
                ContentBuildPipeline.CreateAssets(new Dictionary<string, string>(), configuredAssets);
                Assert.Fail("An unassigned content asset should stop manifest generation.");
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>
        /// Verifies that build options own an immutable snapshot of configured address mappings.
        /// </summary>
        [Test]
        public void Constructor_ClonesConfiguredAssets()
        {
            var configuredAssets = new[]
            {
                new ContentBuildAsset("UI/Login", "Assets/UI/Login.prefab")
            };
            var options = new ContentBuildOptions("1", "Build", BuildTarget.StandaloneWindows64, assets: configuredAssets);

            configuredAssets[0] = new ContentBuildAsset("Changed", "Assets/Changed.asset");

            Assert.AreEqual("UI/Login", options.Assets[0].Address);
        }

        /// <summary>
        /// Verifies the complete Unity AssetBundle build, hash, mapping, and JSON manifest output.
        /// </summary>
        [Test]
        public void Build_WithAssignedAssetWritesCompatibleManifest()
        {
            var outputPath = Path.Combine(Path.GetTempPath(), "TritoneContentBuildTests", Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(TestAssetFolder);
                File.WriteAllText(TestAssetPath, "tritone-content-build");
                AssetDatabase.ImportAsset(TestAssetPath, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(TestAssetPath);
                Assert.IsNotNull(importer);
                importer.assetBundleName = "tests/content";
                importer.SaveAndReimport();

                var mappings = new[] { new ContentBuildAsset("Tests/Payload", TestAssetPath) };
                var result = ContentBuildPipeline.Build(new ContentBuildOptions("test-1", outputPath, BuildTarget.StandaloneWindows64, assets: mappings));
                var serialized = File.ReadAllText(result.ManifestPath);
                var restored = new UnityJsonContentManifestSerializer().Deserialize(serialized);

                Assert.AreEqual("test-1", restored.Version);
                Assert.AreEqual(1, restored.Bundles.Count);
                Assert.AreEqual(64, restored.Bundles[0].Hash.Length);
                Assert.Greater(restored.Bundles[0].Size, 0);
                Assert.AreEqual("Tests/Payload", restored.Assets[0].Address);
                Assert.AreEqual("tests/content", restored.Assets[0].BundleName);
            }
            finally
            {
                AssetDatabase.RemoveAssetBundleName("tests/content", true);
                AssetDatabase.DeleteAsset(TestAssetFolder);
                if (Directory.Exists(outputPath))
                    Directory.Delete(outputPath, true);
            }
        }
    }
}
