using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Assets;
using Tritone.Kernel;
using Tritone.Localization;
using Tritone.Unity.Assets;
using Tritone.Unity.Localization;
using Tritone.Unity.Tables;
using UnityEngine;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies localized lookup, fallback, language switching, and table release.
    /// </summary>
    public sealed class LocalizationTests
    {
        /// <summary>
        /// Verifies language tables switch while missing keys remain readable.
        /// </summary>
        [Test]
        public void LocalizationService_SwitchesTablesAndFallsBackToKey()
        {
            LocalizationAssetProvider provider = new();
            var application = new GameApplicationBuilder()
                .UseAssets(provider)
                .UseTables()
                .UseLocalization("en")
                .Build();
            application.Start();
            var localization = application.Services.GetRequired<ILocalizationService>();

            Assert.AreEqual("Hello", localization.Get("Greeting"));
            Assert.AreEqual("Missing", localization.Get("Missing"));
            localization.SetLanguage("zh-CN");
            Assert.AreEqual("你好", localization.Get("Greeting"));
            Assert.AreEqual("zh-CN", localization.Language);
            Assert.AreEqual(1, provider.ReleaseCount);
            application.Stop();
            Assert.AreEqual(2, provider.ReleaseCount);
        }

        /// <summary>
        /// Provides deterministic JSON language table assets.
        /// </summary>
        private sealed class LocalizationAssetProvider : IAssetProvider
        {
            // Stores stable text assets by provider path.
            private readonly Dictionary<string, TextAsset> mAssets = new()
            {
                ["Localization/en"] =
                    new TextAsset("{\"Rows\":[{\"Id\":\"Greeting\",\"Text\":\"Hello\"}]}"),
                ["Localization/zh-CN"] =
                    new TextAsset("{\"Rows\":[{\"Id\":\"Greeting\",\"Text\":\"你好\"}]}")
            };

            // Gets the number of released table assets.
            internal int ReleaseCount { get; private set; }

            /// <inheritdoc />
            public object Load(string path, Type assetType)
            {
                return mAssets[path];
            }

            /// <inheritdoc />
            public Task<object> LoadAsync(string path, Type assetType)
            {
                return Task.FromResult((object)mAssets[path]);
            }

            /// <inheritdoc />
            public void Release(object asset)
            {
                ReleaseCount++;
            }
        }
    }
}
