using System;
using System.Threading;
using NUnit.Framework;
using Tritone.Kernel;
using Tritone.Unity.Assets;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies Addressables catalog checks, locator activation, and builder integration.
    /// </summary>
    public sealed class AddressablesCatalogTests
    {
        /// <summary>
        /// Verifies that an unchanged remote hash avoids an unnecessary catalog update.
        /// </summary>
        [Test]
        public void UpdateCatalogsAsync_WithoutChangesSkipsLocatorUpdate()
        {
            FakeAddressablesCatalogBackend backend = new(Array.Empty<string>(), Array.Empty<string>());
            using var application                  = CreateApplication(backend);
            var service                            = application.Services.GetRequired<IAddressablesCatalogService>();

            var result = service.UpdateCatalogsAsync().GetAwaiter().GetResult();

            Assert.IsFalse(result.Updated);
            Assert.AreEqual(0, result.CatalogIds.Count);
            Assert.AreEqual(0, result.LocatorIds.Count);
            Assert.AreEqual(1, backend.CheckCount);
            Assert.AreEqual(0, backend.UpdateCount);
        }

        /// <summary>
        /// Verifies that changed catalogs activate and report their replacement locators.
        /// </summary>
        [Test]
        public void UpdateCatalogsAsync_WithChangesActivatesLatestLocators()
        {
            var catalogs                            = new[] { "catalog-main", "catalog-events" };
            var locators                            = new[] { "main-v2", "events-v5" };
            FakeAddressablesCatalogBackend backend = new(catalogs, locators);
            using var application                  = CreateApplication(backend);
            var service                            = application.Services.GetRequired<IAddressablesCatalogService>();

            var result = service.UpdateCatalogsAsync().GetAwaiter().GetResult();

            Assert.IsTrue(result.Updated);
            CollectionAssert.AreEqual(catalogs, result.CatalogIds);
            CollectionAssert.AreEqual(locators, result.LocatorIds);
            Assert.AreEqual(1, backend.CheckCount);
            Assert.AreEqual(1, backend.UpdateCount);
            CollectionAssert.AreEqual(catalogs, backend.LastUpdatedCatalogIds);
        }

        /// <summary>
        /// Verifies that cancellation prevents a catalog check from starting.
        /// </summary>
        [Test]
        public void UpdateCatalogsAsync_CancelledRequestDoesNotReachBackend()
        {
            FakeAddressablesCatalogBackend backend = new(Array.Empty<string>(), Array.Empty<string>());
            using var application                  = CreateApplication(backend);
            var service                            = application.Services.GetRequired<IAddressablesCatalogService>();
            using var source                       = new CancellationTokenSource();
            source.Cancel();

            var cancelled = false;
            try
            {
                service.UpdateCatalogsAsync(source.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            Assert.IsTrue(cancelled);
            Assert.AreEqual(0, backend.CheckCount);
        }

        /// <summary>
        /// Creates one started application using the supplied catalog backend.
        /// </summary>
        /// <param name="backend">The deterministic catalog backend.</param>
        /// <returns>A started application owning Addressables services.</returns>
        private static GameApplication CreateApplication(IAddressablesCatalogBackend backend)
        {
            GameApplicationBuilder builder = new();
            var application                = builder.UseAddressableAssets(backend).Build();
            application.Start();
            return application;
        }
    }
}
