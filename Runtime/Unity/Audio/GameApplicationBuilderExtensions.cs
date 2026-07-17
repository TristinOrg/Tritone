using System;
using Tritone.Kernel;
using Tritone.Unity.Assets;

namespace Tritone.Unity.Audio
{
    /// <summary>
    /// Provides shared Unity audio setup for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds shared music and sound-effect playback.
        /// </summary>
        /// <param name="builder">The application builder receiving audio infrastructure.</param>
        /// <returns>The same application builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseAudio(this GameApplicationBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.AddModule(new AudioModule(), typeof(AssetModule));
        }
    }
}
