using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Content;
using Tritone.Kernel;
using Tritone.Unity.ContentUpdates;
using UnityEngine.TestTools;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies transactional downloads, verification, recovery, and lifecycle-managed usage.
    /// </summary>
    public sealed class ContentUpdateExecutionTests
    {
        // Stores isolated file storage for the current test.
        private string mRootPath;

        // Stores the JSON serializer shared by test manifests.
        private UnityJsonContentManifestSerializer mSerializer;

        /// <summary>
        /// Creates isolated local content storage before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mRootPath  = Path.Combine(Path.GetTempPath(), "Tritone", Guid.NewGuid().ToString("N"));
            mSerializer = new();
            Directory.CreateDirectory(mRootPath);
        }

        /// <summary>
        /// Removes isolated local content storage after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(mRootPath))
                Directory.Delete(mRootPath, true);
        }

        /// <summary>
        /// Verifies first installation, durable manifest storage, progress, and unchanged rechecks.
        /// </summary>
        [UnityTest]
        public IEnumerator UpdateAsync_FirstInstallCommitsAndSecondCheckReusesFiles()
        {
            return RunAsync(UpdateAsync_FirstInstallCommitsAndSecondCheckReusesFilesBody);
        }

        /// <summary>
        /// Executes the asynchronous first-installation test body.
        /// </summary>
        /// <returns>A task completed after all assertions run.</returns>
        private async Task UpdateAsync_FirstInstallCommitsAndSecondCheckReusesFilesBody()
        {
            var bundleBytes = new byte[] { 1, 2, 3, 4, 5 };
            var manifest    = CreateManifest("1", "ui.bundle", bundleBytes);
            MemoryContentUpdateSource source = new(mSerializer);
            source.SetContent(manifest, "ui.bundle", bundleBytes);
            ContentUpdateSettings settings = new(mRootPath);
            ContentUpdater updater          = new(settings, source, mSerializer);
            ContentUpdateProgress lastProgress = default;

            var firstResult = await updater.UpdateAsync(value => lastProgress = value);

            Assert.IsTrue(firstResult.Updated);
            Assert.AreEqual(EContentUpdateStage.Completed, lastProgress.Stage);
            CollectionAssert.AreEqual(bundleBytes,
                                      File.ReadAllBytes(settings.ResolveBundlePath("ui.bundle")));
            Assert.IsTrue(File.Exists(settings.ManifestPath));
            Assert.IsFalse(Directory.Exists(settings.TransactionPath));
            Assert.AreEqual(1, source.DownloadCount);

            source.ResetDownloadCount();
            var secondResult = await updater.UpdateAsync();

            Assert.IsFalse(secondResult.Updated);
            Assert.AreEqual(0, source.DownloadCount);
        }

        /// <summary>
        /// Verifies that a changed same-name bundle safely replaces its installed version.
        /// </summary>
        [UnityTest]
        public IEnumerator UpdateAsync_ChangedBundleReplacesInstalledFile()
        {
            return RunAsync(UpdateAsync_ChangedBundleReplacesInstalledFileBody);
        }

        /// <summary>
        /// Executes the asynchronous same-name replacement test body.
        /// </summary>
        /// <returns>A task completed after all assertions run.</returns>
        private async Task UpdateAsync_ChangedBundleReplacesInstalledFileBody()
        {
            var firstBytes  = new byte[] { 1, 1, 1 };
            var secondBytes = new byte[] { 2, 2, 2, 2 };
            ContentUpdateSettings settings = new(mRootPath);
            MemoryContentUpdateSource source = new(mSerializer);
            ContentUpdater updater = new(settings, source, mSerializer);
            source.SetContent(CreateManifest("1", "ui.bundle", firstBytes),
                              "ui.bundle",
                              firstBytes);
            await updater.UpdateAsync();

            source.SetContent(CreateManifest("2", "ui.bundle", secondBytes),
                              "ui.bundle",
                              secondBytes);
            var result = await updater.UpdateAsync();

            Assert.IsTrue(result.Updated);
            Assert.AreEqual("1", result.PreviousManifest.Version);
            Assert.AreEqual("2", result.ActiveManifest.Version);
            Assert.AreEqual(1, result.Plan.Downloads.Count);
            CollectionAssert.AreEqual(secondBytes,
                                      File.ReadAllBytes(settings.ResolveBundlePath("ui.bundle")));
        }

        /// <summary>
        /// Verifies that a failed SHA-256 check preserves the complete installed version.
        /// </summary>
        [UnityTest]
        public IEnumerator UpdateAsync_HashMismatchPreservesInstalledContent()
        {
            return RunAsync(UpdateAsync_HashMismatchPreservesInstalledContentBody);
        }

        /// <summary>
        /// Executes the asynchronous hash-mismatch rollback test body.
        /// </summary>
        /// <returns>A task completed after all assertions run.</returns>
        private async Task UpdateAsync_HashMismatchPreservesInstalledContentBody()
        {
            var firstBytes   = new byte[] { 1, 2, 3 };
            var expectedBytes = new byte[] { 4, 5, 6 };
            var corruptBytes = new byte[] { 7, 8, 9 };
            ContentUpdateSettings settings = new(mRootPath);
            MemoryContentUpdateSource source = new(mSerializer);
            ContentUpdater updater = new(settings, source, mSerializer);
            source.SetContent(CreateManifest("1", "ui.bundle", firstBytes),
                              "ui.bundle",
                              firstBytes);
            await updater.UpdateAsync();

            source.SetContent(CreateManifest("2", "ui.bundle", expectedBytes),
                              "ui.bundle",
                              corruptBytes);

            var hashMismatchThrown = false;
            try
            {
                await updater.UpdateAsync();
            }
            catch (InvalidDataException)
            {
                hashMismatchThrown = true;
            }

            Assert.IsTrue(hashMismatchThrown);
            CollectionAssert.AreEqual(firstBytes,
                                      File.ReadAllBytes(settings.ResolveBundlePath("ui.bundle")));
            var localManifest = mSerializer.Deserialize(
                File.ReadAllText(settings.ManifestPath));
            Assert.AreEqual("1", localManifest.Version);
            Assert.IsFalse(Directory.Exists(settings.TransactionPath));
        }

        /// <summary>
        /// Verifies that missing physical files are downloaded even when manifest metadata matches.
        /// </summary>
        [UnityTest]
        public IEnumerator UpdateAsync_MissingInstalledFileForcesDownload()
        {
            return RunAsync(UpdateAsync_MissingInstalledFileForcesDownloadBody);
        }

        /// <summary>
        /// Executes the asynchronous missing-file repair test body.
        /// </summary>
        /// <returns>A task completed after all assertions run.</returns>
        private async Task UpdateAsync_MissingInstalledFileForcesDownloadBody()
        {
            var bundleBytes = new byte[] { 3, 1, 4, 1, 5 };
            var manifest    = CreateManifest("1", "ui.bundle", bundleBytes);
            ContentUpdateSettings settings = new(mRootPath);
            MemoryContentUpdateSource source = new(mSerializer);
            source.SetContent(manifest, "ui.bundle", bundleBytes);
            ContentUpdater updater = new(settings, source, mSerializer);
            await updater.UpdateAsync();
            File.Delete(settings.ResolveBundlePath("ui.bundle"));
            source.ResetDownloadCount();

            var result = await updater.UpdateAsync();

            Assert.IsTrue(result.Updated);
            Assert.IsFalse(result.Plan.ManifestChanged);
            Assert.AreEqual(1, source.DownloadCount);
            CollectionAssert.AreEqual(bundleBytes,
                                      File.ReadAllBytes(settings.ResolveBundlePath("ui.bundle")));
        }

        /// <summary>
        /// Verifies that an interrupted commit is rolled back before the next remote check.
        /// </summary>
        [UnityTest]
        public IEnumerator UpdateAsync_InterruptedTransactionRestoresPreviousVersion()
        {
            return RunAsync(UpdateAsync_InterruptedTransactionRestoresPreviousVersionBody);
        }

        /// <summary>
        /// Executes the asynchronous interrupted-transaction recovery test body.
        /// </summary>
        /// <returns>A task completed after all assertions run.</returns>
        private async Task UpdateAsync_InterruptedTransactionRestoresPreviousVersionBody()
        {
            var oldBytes = new byte[] { 1, 2, 3 };
            var newBytes = new byte[] { 4, 5, 6 };
            var oldManifest = CreateManifest("1", "ui.bundle", oldBytes);
            var newManifest = CreateManifest("2", "ui.bundle", newBytes);
            ContentUpdateSettings settings = new(mRootPath);
            File.WriteAllBytes(settings.ResolveBundlePath("ui.bundle"), newBytes);
            File.WriteAllText(settings.ManifestPath, mSerializer.Serialize(newManifest));

            var backupRoot = Path.Combine(settings.TransactionPath, "backups");
            Directory.CreateDirectory(backupRoot);
            File.WriteAllBytes(Path.Combine(backupRoot, "ui.bundle"), oldBytes);
            File.WriteAllText(Path.Combine(settings.TransactionPath, "manifest.backup"),
                              mSerializer.Serialize(oldManifest));

            MemoryContentUpdateSource source = new(mSerializer);
            source.SetContent(oldManifest, "ui.bundle", oldBytes);
            ContentUpdater updater = new(settings, source, mSerializer);

            var result = await updater.UpdateAsync();

            Assert.IsFalse(result.Updated);
            CollectionAssert.AreEqual(oldBytes,
                                      File.ReadAllBytes(settings.ResolveBundlePath("ui.bundle")));
            Assert.IsFalse(Directory.Exists(settings.TransactionPath));
        }

        /// <summary>
        /// Verifies that the fluent builder and ModuleBase helper hide update and asset activation details.
        /// </summary>
        [UnityTest]
        public IEnumerator ModuleBase_UpdateContentAsyncActivatesAssetLoading()
        {
            return RunAsync(ModuleBase_UpdateContentAsyncActivatesAssetLoadingBody);
        }

        /// <summary>
        /// Executes the asynchronous ModuleBase integration test body.
        /// </summary>
        /// <returns>A task completed after all assertions run.</returns>
        private async Task ModuleBase_UpdateContentAsyncActivatesAssetLoadingBody()
        {
            var bundleBytes = new byte[] { 9, 8, 7 };
            var manifest = new ContentManifest(
                "1",
                new[] { CreateBundle("ui", "ui.bundle", bundleBytes) },
                new[] { new ContentAsset("UI/Login", "ui", "Assets/UI/Login.prefab") });
            MemoryContentUpdateSource updateSource = new(mSerializer);
            updateSource.SetContent(manifest, "ui.bundle", bundleBytes);
            FakeBundleSource bundleSource = new();
            bundleSource.AddAsset("ui",
                                  "Assets/UI/Login.prefab",
                                  new BundleTestAsset("Login"));
            ContentUpdateSettings settings = new(mRootPath);
            ContentTestModule testModule = new();
            GameApplicationBuilder builder = new();
            builder.UseContentAssets(settings,
                                     updateSource,
                                     mSerializer,
                                     bundleSource);
            builder.AddModule(testModule);
            using var application = builder.Build();
            application.Start();

            await testModule.UpdateContent();
            var asset = testModule.LoadLogin();

            Assert.AreEqual("Login", asset.Name);

            var updateRejected = false;
            try
            {
                await testModule.UpdateContent();
            }
            catch (InvalidOperationException)
            {
                updateRejected = true;
            }

            Assert.IsTrue(updateRejected);
            Assert.IsTrue(testModule.ReleaseLogin(asset));
        }

        /// <summary>
        /// Advances one asynchronous test without blocking the Unity synchronization context.
        /// </summary>
        /// <param name="testBody">The asynchronous assertions to execute.</param>
        /// <returns>An enumerator that waits one frame while work remains incomplete.</returns>
        private static IEnumerator RunAsync(Func<Task> testBody)
        {
            var task = testBody.Invoke();
            while (!task.IsCompleted)
                yield return null;

            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates one single-bundle manifest using the exact SHA-256 of supplied bytes.
        /// </summary>
        /// <param name="version">The manifest version label.</param>
        /// <param name="fileName">The portable bundle file name.</param>
        /// <param name="bytes">The complete expected bundle bytes.</param>
        /// <returns>A validated content manifest.</returns>
        private static ContentManifest CreateManifest(string version,
                                                      string fileName,
                                                      byte[] bytes)
        {
            return new ContentManifest(
                version,
                new[] { CreateBundle("ui", fileName, bytes) },
                Array.Empty<ContentAsset>());
        }

        /// <summary>
        /// Creates one content bundle using the exact SHA-256 of supplied bytes.
        /// </summary>
        /// <param name="name">The stable logical bundle name.</param>
        /// <param name="fileName">The portable bundle file name.</param>
        /// <param name="bytes">The complete expected bundle bytes.</param>
        /// <returns>A validated content bundle.</returns>
        private static ContentBundle CreateBundle(string name,
                                                  string fileName,
                                                  byte[] bytes)
        {
            return new ContentBundle(name,
                                     fileName,
                                     ComputeHash(bytes),
                                     bytes.LongLength);
        }

        /// <summary>
        /// Computes lowercase SHA-256 text for deterministic in-memory test data.
        /// </summary>
        /// <param name="bytes">The bytes to hash.</param>
        /// <returns>The lowercase hexadecimal SHA-256 hash.</returns>
        private static string ComputeHash(byte[] bytes)
        {
            using SHA256 algorithm = SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(bytes))
                               .Replace("-", string.Empty)
                               .ToLowerInvariant();
        }
    }

    /// <summary>
    /// Provides deterministic in-memory remote content while writing caller-owned temporary files.
    /// </summary>
    internal sealed class MemoryContentUpdateSource : IContentUpdateSource
    {
        // Stores bundle bytes by portable relative file name.
        private readonly Dictionary<string, byte[]> mBundles = new(StringComparer.Ordinal);

        // Stores the manifest serializer used by tests.
        private readonly IContentManifestSerializer mSerializer;

        // Stores the serialized remote manifest.
        private string mManifestContent;

        // Gets the number of completed bundle downloads.
        internal int DownloadCount { get; private set; }

        /// <summary>
        /// Initializes one empty deterministic source.
        /// </summary>
        /// <param name="serializer">The serializer used to publish test manifests.</param>
        internal MemoryContentUpdateSource(IContentManifestSerializer serializer)
        {
            mSerializer = serializer;
        }

        /// <summary>
        /// Replaces the complete remote manifest and one downloadable bundle.
        /// </summary>
        /// <param name="manifest">The remote manifest.</param>
        /// <param name="fileName">The remote bundle file name.</param>
        /// <param name="bytes">The bytes returned for that file.</param>
        internal void SetContent(ContentManifest manifest, string fileName, byte[] bytes)
        {
            mManifestContent = mSerializer.Serialize(manifest);
            mBundles.Clear();
            mBundles.Add(fileName, (byte[])bytes.Clone());
        }

        /// <summary>
        /// Clears the download counter without changing remote content.
        /// </summary>
        internal void ResetDownloadCount()
        {
            DownloadCount = 0;
        }

        /// <inheritdoc />
        public Task<string> GetManifestAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(mManifestContent);
        }

        /// <inheritdoc />
        public Task DownloadBundleAsync(ContentBundle bundle,
                                        string destinationPath,
                                        Action<long> progress,
                                        CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytes = mBundles[bundle.FileName];
            File.WriteAllBytes(destinationPath, bytes);
            DownloadCount++;
            progress?.Invoke(bytes.LongLength);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Exposes protected ModuleBase content and asset helpers for integration tests.
    /// </summary>
    internal sealed class ContentTestModule : ModuleBase
    {
        /// <summary>
        /// Starts one lifecycle-owned content update.
        /// </summary>
        /// <returns>A task containing the successful update result.</returns>
        internal Task<ContentUpdateResult> UpdateContent()
        {
            return UpdateContentAsync();
        }

        /// <summary>
        /// Loads the addressed login test asset through the activated provider.
        /// </summary>
        /// <returns>The loaded login test asset.</returns>
        internal BundleTestAsset LoadLogin()
        {
            return LoadAsset<BundleTestAsset>("UI/Login");
        }

        /// <summary>
        /// Releases one login test asset before module shutdown.
        /// </summary>
        /// <param name="asset">The loaded login test asset.</param>
        /// <returns>True when the module owned and released the asset.</returns>
        internal bool ReleaseLogin(BundleTestAsset asset)
        {
            return ReleaseAsset(asset);
        }
    }
}
