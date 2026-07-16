using System;
using NUnit.Framework;
using Tritone.Content;
using Tritone.Unity.Assets.AssetBundles;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies content manifest validation, update planning, and AssetBundle registry conversion.
    /// </summary>
    public sealed class ContentUpdateTests
    {
        /// <summary>
        /// Verifies that planning downloads only changed files and removes only unreferenced files.
        /// </summary>
        [Test]
        public void CreatePlan_ReturnsMinimalDeterministicFileChanges()
        {
            var localManifest = CreateLocalManifest();
            var remoteManifest = new ContentManifest(
                "2.0.0",
                new[]
                {
                    new ContentBundle("core", "core.bundle", "CORE-V1", 100),
                    new ContentBundle("ui", "ui.bundle", "ui-v2", 250, "core"),
                    new ContentBundle("shared", "shared.bundle", "shared-v1", 75, "core")
                },
                new[]
                {
                    new ContentAsset("UI/Login", "ui", "Assets/UI/Login.prefab")
                });

            var plan = ContentUpdatePlanner.CreatePlan(localManifest, remoteManifest);

            Assert.IsTrue(plan.HasChanges);
            Assert.AreSame(remoteManifest, plan.TargetManifest);
            Assert.AreEqual(325, plan.DownloadBytes);
            Assert.AreEqual(2, plan.Downloads.Count);
            Assert.AreEqual("ui", plan.Downloads[0].Name);
            Assert.AreEqual("shared", plan.Downloads[1].Name);
            CollectionAssert.AreEqual(new[] { "ui-old.bundle", "unused.bundle" }, plan.ObsoleteFiles);
        }

        /// <summary>
        /// Verifies that a renamed logical bundle reuses an identical local file.
        /// </summary>
        [Test]
        public void CreatePlan_ReusesFileAfterLogicalBundleRename()
        {
            var localManifest = new ContentManifest(
                "1",
                new[] { new ContentBundle("old-shared", "shared.bundle", "same-hash", 64) },
                Array.Empty<ContentAsset>());
            var remoteManifest = new ContentManifest(
                "2",
                new[] { new ContentBundle("new-shared", "shared.bundle", "SAME-HASH", 64) },
                Array.Empty<ContentAsset>());

            var plan = ContentUpdatePlanner.CreatePlan(localManifest, remoteManifest);

            Assert.IsFalse(plan.HasChanges);
            Assert.AreEqual(0, plan.DownloadBytes);
        }

        /// <summary>
        /// Verifies that a first installation downloads every remote bundle in manifest order.
        /// </summary>
        [Test]
        public void CreatePlan_WithoutLocalManifestDownloadsEverything()
        {
            var remoteManifest = new ContentManifest(
                "1",
                new[]
                {
                    new ContentBundle("core", "core.bundle", "core-v1", 100),
                    new ContentBundle("ui", "ui.bundle", "ui-v1", 200, "core")
                },
                Array.Empty<ContentAsset>());

            var plan = ContentUpdatePlanner.CreatePlan(null, remoteManifest);

            Assert.AreEqual(300, plan.DownloadBytes);
            Assert.AreEqual(2, plan.Downloads.Count);
            Assert.AreEqual(0, plan.ObsoleteFiles.Count);
        }

        /// <summary>
        /// Verifies that manifests reject missing bundle dependencies immediately.
        /// </summary>
        [Test]
        public void Constructor_RejectsMissingDependency()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new ContentManifest(
                    "1",
                    new[] { new ContentBundle("ui", "ui.bundle", "hash", 1, "missing") },
                    Array.Empty<ContentAsset>()));
        }

        /// <summary>
        /// Verifies that manifests reject bundle dependency cycles immediately.
        /// </summary>
        [Test]
        public void Constructor_RejectsDependencyCycle()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new ContentManifest(
                    "1",
                    new[]
                    {
                        new ContentBundle("a", "a.bundle", "a", 1, "b"),
                        new ContentBundle("b", "b.bundle", "b", 1, "a")
                    },
                    Array.Empty<ContentAsset>()));
        }

        /// <summary>
        /// Verifies that one manifest creates a provider-ready dependency-aware registry.
        /// </summary>
        [Test]
        public void CreateAssetBundleRegistry_MapsBundlesDependenciesAndAssets()
        {
            var manifest = new ContentManifest(
                "1",
                new[]
                {
                    new ContentBundle("core", "core.bundle", "core-v1", 100),
                    new ContentBundle("ui", "ui.bundle", "ui-v1", 200, "core")
                },
                new[]
                {
                    new ContentAsset("UI/Login", "ui", "Assets/UI/Login.prefab")
                });
            FakeBundleSource source = new();
            source.AddAsset("ui", "Assets/UI/Login.prefab", new BundleTestAsset("Login"));
            AssetBundleAssetProvider provider = new(manifest.CreateAssetBundleRegistry(), source);

            var asset = provider.Load("UI/Login", typeof(BundleTestAsset));

            Assert.AreEqual("Login", ((BundleTestAsset)asset).Name);
            CollectionAssert.AreEqual(new[] { "core", "ui" }, source.SyncLoadOrder);
            provider.Release(asset);
            CollectionAssert.AreEqual(new[] { "ui", "core" }, source.UnloadOrder);
        }

        /// <summary>
        /// Creates the installed manifest used by deterministic update planning tests.
        /// </summary>
        /// <returns>A validated local content manifest.</returns>
        private static ContentManifest CreateLocalManifest()
        {
            return new ContentManifest(
                "1.0.0",
                new[]
                {
                    new ContentBundle("core", "core.bundle", "core-v1", 100),
                    new ContentBundle("ui", "ui-old.bundle", "ui-v1", 200, "core"),
                    new ContentBundle("unused", "unused.bundle", "unused-v1", 50)
                },
                Array.Empty<ContentAsset>());
        }
    }
}
