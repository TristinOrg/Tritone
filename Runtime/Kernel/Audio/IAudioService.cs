using System.Threading.Tasks;

namespace Tritone.Audio
{
    /// <summary>
    /// Provides shared music and sound-effect playback by asset path.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// Gets or sets the normalized master volume.
        /// </summary>
        float MasterVolume { get; set; }

        /// <summary>
        /// Gets or sets the normalized music volume.
        /// </summary>
        float MusicVolume { get; set; }

        /// <summary>
        /// Gets or sets the normalized sound-effect volume.
        /// </summary>
        float SoundVolume { get; set; }

        /// <summary>
        /// Gets or sets whether all framework audio is muted.
        /// </summary>
        bool Muted { get; set; }

        /// <summary>
        /// Loads and starts looping background music.
        /// </summary>
        /// <param name="path">The audio clip asset path.</param>
        void PlayMusic(string path);

        /// <summary>
        /// Loads and starts looping background music asynchronously.
        /// </summary>
        /// <param name="path">The audio clip asset path.</param>
        /// <returns>A task completed after playback starts.</returns>
        Task PlayMusicAsync(string path);

        /// <summary>
        /// Stops the active background music.
        /// </summary>
        void StopMusic();

        /// <summary>
        /// Loads and plays one sound effect.
        /// </summary>
        /// <param name="path">The audio clip asset path.</param>
        /// <returns>A handle that can stop the playback.</returns>
        AudioHandle PlaySound(string path);

        /// <summary>
        /// Loads and plays one sound effect asynchronously.
        /// </summary>
        /// <param name="path">The audio clip asset path.</param>
        /// <returns>A task containing a handle that can stop the playback.</returns>
        Task<AudioHandle> PlaySoundAsync(string path);

        /// <summary>
        /// Stops one active sound effect.
        /// </summary>
        /// <param name="handle">The playback handle.</param>
        /// <returns>True when an active playback was stopped; otherwise, false.</returns>
        bool StopSound(AudioHandle handle);
    }
}
