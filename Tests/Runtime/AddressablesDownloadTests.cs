using System;
using NUnit.Framework;
using Tritone.Kernel;
using Tritone.Unity.Assets;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies Addressables dependency size checks, cache hits, and downloads.
    /// </summary>
    public sealed class AddressablesDownloadTests
    {
        /// <summary>
        /// Verifies that cached dependencies skip the download operation.
        /// </summary>
        [Test]
        public void DownloadAsync_CachedDependenciesSkipDownload()
        {
            FakeAddressablesDownloadBackend backend = new(0);
            using var application                   = CreateApplication(backend);
            var service                             = application.Services.GetRequired<IAddressablesDownloadService>();

            var result = service.DownloadAsync("startup").GetAwaiter().GetResult();

            Assert.IsFalse(result.Downloaded);
            Assert.AreEqual(0, result.DownloadedBytes);
            Assert.AreEqual(1, backend.SizeCheckCount);
            Assert.AreEqual(0, backend.DownloadCount);
        }

        /// <summary>
        /// Verifies that uncached dependencies download and report their measured bytes.
        /// </summary>
        [Test]
        public void DownloadAsync_UncachedDependenciesPopulateCache()
        {
            FakeAddressablesDownloadBackend backend = new(4096);
            using var application                   = CreateApplication(backend);
            var service                             = application.Services.GetRequired<IAddressablesDownloadService>();

            var result = service.DownloadAsync("battle").GetAwaiter().GetResult();

            Assert.IsTrue(result.Downloaded);
            Assert.AreEqual("battle", result.Key);
            Assert.AreEqual(4096, result.DownloadedBytes);
            Assert.AreEqual(1, backend.DownloadCount);
        }

        /// <summary>
        /// Verifies that empty download keys are rejected before reaching the backend.
        /// </summary>
        [Test]
        public void DownloadAsync_EmptyKeyIsRejected()
        {
            FakeAddressablesDownloadBackend backend = new(1);
            using var application                   = CreateApplication(backend);
            var service                             = application.Services.GetRequired<IAddressablesDownloadService>();
            var rejected                            = false;
            try
            {
                service.DownloadAsync(" ").GetAwaiter().GetResult();
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Assert.IsTrue(rejected);
            Assert.AreEqual(0, backend.SizeCheckCount);
        }

        /// <summary>
        /// Creates one started application using the supplied download backend.
        /// </summary>
        /// <param name="downloadBackend">The deterministic dependency backend.</param>
        /// <returns>A started application owning Addressables services.</returns>
        private static GameApplication CreateApplication(IAddressablesDownloadBackend downloadBackend)
        {
            FakeAddressablesCatalogBackend catalogBackend = new(Array.Empty<string>(), Array.Empty<string>());
            GameApplicationBuilder builder                 = new();
            var application                                = builder.UseAddressableAssets(catalogBackend, downloadBackend).Build();
            application.Start();
            return application;
        }
    }
}
