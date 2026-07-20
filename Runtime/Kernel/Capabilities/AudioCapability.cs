using System.Threading.Tasks;
using Tritone.Audio;

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
}
