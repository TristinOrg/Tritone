using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tritone.Audio;
using Tritone.Kernel;
using UnityEngine;

namespace Tritone.Unity.Audio
{
    /// <summary>
    /// Plays shared audio assets through reusable Unity AudioSources.
    /// </summary>
    public sealed class AudioModule : ModuleBase, IAudioService, IUpdateSystem
    {
        // Stores active sound effects by playback identifier.
        private readonly Dictionary<int, SoundPlayback> mSounds = new();

        // Reuses inactive sound-effect sources.
        private readonly Stack<AudioSource> mAvailableSources = new();

        // Reuses completion storage during per-frame sound cleanup.
        private readonly List<int> mCompletedSounds = new();

        // Stores the persistent audio hierarchy.
        private GameObject mRoot;

        // Stores the dedicated background music source.
        private AudioSource mMusicSource;

        // Retains the active music clip asset.
        private AudioClip mMusicClip;

        // Generates positive sound-effect identifiers.
        private int mNextId = 1;

        // Invalidates older asynchronous music requests.
        private int mMusicRequestId;

        // Stores normalized volume values.
        private float mMasterVolume = 1.0f;
        private float mMusicVolume  = 1.0f;
        private float mSoundVolume  = 1.0f;

        // Stores the global mute state.
        private bool mMuted;

        /// <inheritdoc />
        public int Order => 1000;

        /// <inheritdoc />
        public float MasterVolume
        {
            get => mMasterVolume;
            set
            {
                mMasterVolume = ValidateVolume(value);
                ApplyVolumes();
            }
        }

        /// <inheritdoc />
        public float MusicVolume
        {
            get => mMusicVolume;
            set
            {
                mMusicVolume = ValidateVolume(value);
                ApplyVolumes();
            }
        }

        /// <inheritdoc />
        public float SoundVolume
        {
            get => mSoundVolume;
            set
            {
                mSoundVolume = ValidateVolume(value);
                ApplyVolumes();
            }
        }

        /// <inheritdoc />
        public bool Muted
        {
            get => mMuted;
            set
            {
                mMuted = value;
                ApplyVolumes();
            }
        }

        /// <inheritdoc />
        protected override void OnConfigure(IServiceRegistry services)
        {
            mRoot = new GameObject("Tritone.Audio");
            if (Application.isPlaying)
                UnityEngine.Object.DontDestroyOnLoad(mRoot);
            mMusicSource             = mRoot.AddComponent<AudioSource>();
            mMusicSource.playOnAwake = false;
            mMusicSource.loop        = true;
            services.AddSingleton<IAudioService>(this);
        }

        /// <inheritdoc />
        public void PlayMusic(string path)
        {
            mMusicRequestId++;
            var clip = LoadAsset<AudioClip>(path);
            StartMusic(clip);
        }

        /// <inheritdoc />
        public async Task PlayMusicAsync(string path)
        {
            var requestId = ++mMusicRequestId;
            var clip = await LoadAssetAsync<AudioClip>(path);
            if (requestId != mMusicRequestId)
            {
                ReleaseAsset(clip);
                return;
            }
            StartMusic(clip);
        }

        /// <inheritdoc />
        public void StopMusic()
        {
            mMusicRequestId++;
            if (mMusicSource != null)
                mMusicSource.Stop();
            if (mMusicClip != null)
            {
                ReleaseAsset(mMusicClip);
                mMusicClip = null;
            }
            if (mMusicSource != null)
                mMusicSource.clip = null;
        }

        /// <inheritdoc />
        public AudioHandle PlaySound(string path)
        {
            return StartSound(LoadAsset<AudioClip>(path));
        }

        /// <inheritdoc />
        public async Task<AudioHandle> PlaySoundAsync(string path)
        {
            return StartSound(await LoadAssetAsync<AudioClip>(path));
        }

        /// <inheritdoc />
        public bool StopSound(AudioHandle handle)
        {
            if (!handle.IsValid || !mSounds.TryGetValue(handle.Id, out var playback))
                return false;

            ReleaseSound(handle.Id, playback);
            return true;
        }

        /// <inheritdoc />
        public void Update(in FrameTime time)
        {
            if (mSounds.Count == 0)
                return;

            mCompletedSounds.Clear();
            foreach (var pair in mSounds)
            {
                if (pair.Value.Source.isPlaying)
                    continue;
                mCompletedSounds.Add(pair.Key);
            }
            for (int i = 0, cnt = mCompletedSounds.Count; i < cnt; i++)
            {
                var id = mCompletedSounds[i];
                ReleaseSound(id, mSounds[id]);
            }
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            StopMusic();
            foreach (var pair in mSounds)
            {
                pair.Value.Source.Stop();
                ReleaseAsset(pair.Value.Clip);
            }
            mSounds.Clear();
            mAvailableSources.Clear();
            if (mRoot != null)
                UnityObjectUtility.Destroy(mRoot);
            mRoot        = null;
            mMusicSource = null;
        }

        /// <summary>
        /// Replaces the active music while releasing its previous asset reference.
        /// </summary>
        /// <param name="clip">The newly loaded music clip.</param>
        private void StartMusic(AudioClip clip)
        {
            StopMusic();
            mMusicClip        = clip;
            mMusicSource.clip = clip;
            mMusicSource.Play();
            ApplyVolumes();
        }

        /// <summary>
        /// Starts one sound effect through a reusable source.
        /// </summary>
        /// <param name="clip">The loaded sound-effect clip.</param>
        /// <returns>The new playback handle.</returns>
        private AudioHandle StartSound(AudioClip clip)
        {
            var source = mAvailableSources.Count > 0
                ? mAvailableSources.Pop()
                : mRoot.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop        = false;
            source.clip        = clip;
            source.mute        = mMuted;
            source.volume      = mMasterVolume * mSoundVolume;
            source.Play();

            if (mNextId == int.MaxValue)
                mNextId = 1;
            while (mSounds.ContainsKey(mNextId))
                mNextId++;
            var id = mNextId++;
            mSounds.Add(id, new SoundPlayback(source, clip));
            return new AudioHandle(id);
        }

        /// <summary>
        /// Stops and recycles one sound source while releasing its clip.
        /// </summary>
        /// <param name="id">The active playback identifier.</param>
        /// <param name="playback">The active playback state.</param>
        private void ReleaseSound(int id, SoundPlayback playback)
        {
            mSounds.Remove(id);
            playback.Source.Stop();
            playback.Source.clip = null;
            mAvailableSources.Push(playback.Source);
            ReleaseAsset(playback.Clip);
        }

        /// <summary>
        /// Applies current volume and mute state to every source.
        /// </summary>
        private void ApplyVolumes()
        {
            if (mMusicSource != null)
            {
                mMusicSource.mute   = mMuted;
                mMusicSource.volume = mMasterVolume * mMusicVolume;
            }
            foreach (var pair in mSounds)
            {
                pair.Value.Source.mute   = mMuted;
                pair.Value.Source.volume = mMasterVolume * mSoundVolume;
            }
        }

        /// <summary>
        /// Validates one normalized volume.
        /// </summary>
        /// <param name="value">The requested volume.</param>
        /// <returns>The validated volume.</returns>
        private static float ValidateVolume(float value)
        {
            if (value < 0.0f || value > 1.0f || float.IsNaN(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            return value;
        }

        /// <summary>
        /// Stores one active sound source and its owned clip reference.
        /// </summary>
        private sealed class SoundPlayback
        {
            // Stores the active reusable source.
            internal readonly AudioSource Source;

            // Stores the loaded clip released after playback.
            internal readonly AudioClip Clip;

            /// <summary>
            /// Initializes one sound playback record.
            /// </summary>
            /// <param name="source">The active audio source.</param>
            /// <param name="clip">The owned audio clip reference.</param>
            internal SoundPlayback(AudioSource source, AudioClip clip)
            {
                Source = source;
                Clip   = clip;
            }
        }
    }
}
