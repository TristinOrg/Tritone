using System;
using System.IO;
using NUnit.Framework;
using Tritone.Kernel;
using Tritone.Saves;
using Tritone.Settings;
using Tritone.Unity.Saves;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies typed settings, change notifications, and shutdown persistence.
    /// </summary>
    public sealed class SettingsTests
    {
        /// <summary>
        /// Verifies settings persist automatically and reload with their original types.
        /// </summary>
        [Test]
        public void SettingsService_PersistsDirtyValuesOnStop()
        {
            var root = Path.Combine(Path.GetTempPath(), "TritoneSettings", Guid.NewGuid().ToString("N"));
            try
            {
                var first = CreateApplication(root);
                var settings = first.Services.GetRequired<ISettingsService>();
                var changedKey = string.Empty;
                settings.Changed.Bind(key => changedKey = key);
                settings.SetInt("Quality", 3);
                settings.SetFloat("Volume", 0.75f);
                settings.SetBool("Muted", true);
                settings.SetString("Language", "zh-CN");
                Assert.AreEqual("Language", changedKey);
                first.Stop();

                var second = CreateApplication(root);
                settings = second.Services.GetRequired<ISettingsService>();
                Assert.AreEqual(3, settings.GetInt("Quality"));
                Assert.AreEqual(0.75f, settings.GetFloat("Volume"));
                Assert.IsTrue(settings.GetBool("Muted"));
                Assert.AreEqual("zh-CN", settings.GetString("Language"));
                second.Stop();
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        /// <summary>
        /// Creates one application containing JSON saves and typed settings.
        /// </summary>
        private static GameApplication CreateApplication(string root)
        {
            var builder = new GameApplicationBuilder();
            var saves   = new SaveModule(root, new UnityJsonSaveSerializer());
            return builder.AddModule(saves)
                          .AddModule(new SettingsModule(), typeof(SaveModule))
                          .BuildAndStart();
        }
    }

    /// <summary>
    /// Provides compact test-only application startup syntax.
    /// </summary>
    internal static class TestApplicationExtensions
    {
        /// <summary>
        /// Builds and starts one application.
        /// </summary>
        internal static GameApplication BuildAndStart(this GameApplicationBuilder builder)
        {
            var application = builder.Build();
            application.Start();
            return application;
        }
    }
}
