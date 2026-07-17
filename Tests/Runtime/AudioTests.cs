using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Assets;
using Tritone.Audio;
using Tritone.Kernel;
using Tritone.Unity.Assets;
using Tritone.Unity.Audio;
using UnityEngine;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies simple audio playback, volume validation, and clip release.
    /// </summary>
    public sealed class AudioTests
    {
        /// <summary>
        /// Verifies reusable playback handles and final asset cleanup.
        /// </summary>
        [Test]
        public void AudioService_PlaysStopsAndReleasesClips()
        {
            AudioAssetProvider provider = new();
            var application = new GameApplicationBuilder()
                .UseAssets(provider)
                .UseAudio()
                .Build();
            application.Start();
            var audio = application.Services.GetRequired<IAudioService>();

            audio.MasterVolume = 0.5f;
            audio.MusicVolume  = 0.8f;
            audio.SoundVolume  = 0.6f;
            audio.Muted        = true;
            audio.PlayMusic("Audio/Music");
            var handle = audio.PlaySound("Audio/Click");

            Assert.IsTrue(handle.IsValid);
            Assert.IsTrue(audio.StopSound(handle));
            Assert.IsFalse(audio.StopSound(handle));
            audio.StopMusic();
            Assert.AreEqual(2, provider.ReleaseCount);
            application.Stop();
        }

        /// <summary>
        /// Verifies normalized volume validation.
        /// </summary>
        [Test]
        public void AudioService_RejectsInvalidVolume()
        {
            AudioAssetProvider provider = new();
            var application = new GameApplicationBuilder()
                .UseAssets(provider)
                .UseAudio()
                .Build();
            application.Start();
            var audio = application.Services.GetRequired<IAudioService>();

            Assert.Throws<ArgumentOutOfRangeException>(() => audio.MasterVolume = 1.1f);
            application.Stop();
        }

        /// <summary>
        /// Provides newly created Unity audio clips and records their releases.
        /// </summary>
        private sealed class AudioAssetProvider : IAssetProvider
        {
            // Gets the number of released clip references.
            internal int ReleaseCount { get; private set; }

            /// <inheritdoc />
            public object Load(string path, Type assetType)
            {
                return AudioClip.Create(path, 16, 1, 8000, false);
            }

            /// <inheritdoc />
            public Task<object> LoadAsync(string path, Type assetType)
            {
                return Task.FromResult(Load(path, assetType));
            }

            /// <inheritdoc />
            public void Release(object asset)
            {
                ReleaseCount++;
                UnityEngine.Object.DestroyImmediate((AudioClip)asset);
            }
        }
    }
}
