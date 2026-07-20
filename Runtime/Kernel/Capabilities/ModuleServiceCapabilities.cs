using System;
using System.Threading.Tasks;
using Tritone.Audio;
using Tritone.Localization;
using Tritone.Saves;
using Tritone.Scenes;
using Tritone.Settings;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides shared audio operations without owning playback implementation.
    /// </summary>
    public sealed class AudioCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        /// <summary>
        /// Initializes audio operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal AudioCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Starts looping background music.
        /// </summary>
        /// <param name="path">The audio clip asset path.</param>
        public void PlayMusic(string path)
        {
            GetService().PlayMusic(path);
        }

        /// <summary>
        /// Loads and starts looping background music asynchronously.
        /// </summary>
        /// <param name="path">The audio clip asset path.</param>
        /// <returns>A task completed after playback starts.</returns>
        public Task PlayMusicAsync(string path)
        {
            return GetService().PlayMusicAsync(path);
        }

        /// <summary>
        /// Stops active background music.
        /// </summary>
        public void StopMusic()
        {
            GetService().StopMusic();
        }

        /// <summary>
        /// Plays one sound effect.
        /// </summary>
        /// <param name="path">The audio clip asset path.</param>
        /// <returns>The playback handle.</returns>
        public AudioHandle PlaySound(string path)
        {
            return GetService().PlaySound(path);
        }

        /// <summary>
        /// Loads and plays one sound effect asynchronously.
        /// </summary>
        /// <param name="path">The audio clip asset path.</param>
        /// <returns>A task containing the playback handle.</returns>
        public Task<AudioHandle> PlaySoundAsync(string path)
        {
            return GetService().PlaySoundAsync(path);
        }

        /// <summary>
        /// Stops one active sound effect.
        /// </summary>
        /// <param name="handle">The active playback handle.</param>
        /// <returns>True when an active sound was stopped; otherwise, false.</returns>
        public bool StopSound(AudioHandle handle)
        {
            return GetService().StopSound(handle);
        }

        /// <summary>
        /// Gets the configured audio service.
        /// </summary>
        /// <returns>The application audio service.</returns>
        private IAudioService GetService()
        {
            return mContext.GetRequired<IAudioService>(
                "Audio infrastructure is not configured. Call builder.UseAudio() before adding game modules.");
        }
    }

    /// <summary>
    /// Provides shared save operations without owning storage implementation.
    /// </summary>
    public sealed class SaveCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        /// <summary>
        /// Initializes save operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal SaveCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Writes one strongly typed save slot.
        /// </summary>
        /// <typeparam name="T">The save data type.</typeparam>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <param name="value">The complete save data.</param>
        public void Save<T>(string slot, T value) where T : class
        {
            GetService().Save(slot, value);
        }

        /// <summary>
        /// Loads one required strongly typed save slot.
        /// </summary>
        /// <typeparam name="T">The save data type.</typeparam>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <returns>The loaded save data.</returns>
        public T Load<T>(string slot) where T : class
        {
            return GetService().Load<T>(slot);
        }

        /// <summary>
        /// Attempts to load one optional strongly typed save slot.
        /// </summary>
        /// <typeparam name="T">The save data type.</typeparam>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <param name="value">The loaded value when found; otherwise, null.</param>
        /// <returns>True when the slot was loaded; otherwise, false.</returns>
        public bool TryLoad<T>(string slot, out T value) where T : class
        {
            return GetService().TryLoad(slot, out value);
        }

        /// <summary>
        /// Deletes one local save slot.
        /// </summary>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <returns>True when slot data was deleted; otherwise, false.</returns>
        public bool Delete(string slot)
        {
            return GetService().Delete(slot);
        }

        /// <summary>
        /// Gets the configured save service.
        /// </summary>
        /// <returns>The application save service.</returns>
        private ISaveService GetService()
        {
            return mContext.GetRequired<ISaveService>(
                "Save infrastructure is not configured. Call builder.UseSaves() before adding game modules.");
        }
    }

    /// <summary>
    /// Provides access to the configured typed application settings.
    /// </summary>
    public sealed class SettingsCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        /// <summary>
        /// Initializes settings access for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal SettingsCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Gets the configured typed settings service.
        /// </summary>
        public ISettingsService Service =>
            mContext.GetRequired<ISettingsService>(
                "Settings infrastructure is not configured. Call builder.UseSettings() before adding game modules.");
    }

    /// <summary>
    /// Provides localization lookup and language switching operations.
    /// </summary>
    public sealed class LocalizationCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        /// <summary>
        /// Initializes localization operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal LocalizationCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Gets localized text for one stable key.
        /// </summary>
        /// <param name="key">The stable localization key.</param>
        /// <returns>The active localized text.</returns>
        public string Get(string key)
        {
            return GetService().Get(key);
        }

        /// <summary>
        /// Formats localized text with supplied values.
        /// </summary>
        /// <param name="key">The stable localization key.</param>
        /// <param name="arguments">The format arguments.</param>
        /// <returns>The formatted localized text.</returns>
        public string Format(string key, params object[] arguments)
        {
            return GetService().Format(key, arguments);
        }

        /// <summary>
        /// Loads and activates one language.
        /// </summary>
        /// <param name="language">The target language identifier.</param>
        /// <returns>A task completed after activation.</returns>
        public Task SetLanguageAsync(string language)
        {
            return GetService().SetLanguageAsync(language);
        }

        /// <summary>
        /// Gets the configured localization service.
        /// </summary>
        /// <returns>The application localization service.</returns>
        private ILocalizationService GetService()
        {
            return mContext.GetRequired<ILocalizationService>(
                "Localization is not configured. Call builder.UseLocalization() before adding game modules.");
        }
    }

    /// <summary>
    /// Provides scene and scene-module switching operations.
    /// </summary>
    public sealed class SceneCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        /// <summary>
        /// Initializes scene operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal SceneCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Stops the current scene module and enters one registered module.
        /// </summary>
        /// <typeparam name="TModule">The registered scene module type.</typeparam>
        /// <returns>The newly active scene module.</returns>
        public TModule SwitchModule<TModule>() where TModule : class, IModule
        {
            var service = mContext.GetRequired<ISceneModuleService>(
                "Scene module infrastructure is unavailable.");
            return (TModule)service.SwitchModule(typeof(TModule));
        }

        /// <summary>
        /// Loads a Unity scene and switches its registered scene module.
        /// </summary>
        /// <typeparam name="TModule">The registered scene module type.</typeparam>
        /// <param name="sceneName">The Unity scene name or path.</param>
        /// <param name="progress">The optional normalized progress callback.</param>
        /// <returns>A task containing the active scene module.</returns>
        public Task<TModule> SwitchAsync<TModule>(string sceneName,
                                                  Action<float> progress)
            where TModule : class, IModule
        {
            var service = mContext.GetRequired<ISceneService>(
                "Scene infrastructure is not configured. Call builder.UseScenes() before adding game modules.");
            return service.SwitchAsync<TModule>(sceneName, progress);
        }
    }
}
