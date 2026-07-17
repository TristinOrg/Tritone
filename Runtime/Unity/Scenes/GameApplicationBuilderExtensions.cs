using System;
using Tritone.Kernel;

namespace Tritone.Unity.Scenes
{
    /// <summary>
    /// Provides Unity scene management setup for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds scene management backed by Unity SceneManager.
        /// </summary>
        /// <param name="builder">The application builder receiving scene infrastructure.</param>
        /// <returns>The same application builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseScenes(this GameApplicationBuilder builder)
        {
            return UseScenes(builder, new UnitySceneBackend());
        }

        /// <summary>
        /// Adds scene management backed by a replaceable scene backend.
        /// </summary>
        /// <param name="builder">The application builder receiving scene infrastructure.</param>
        /// <param name="backend">The backend executing concrete scene operations.</param>
        /// <returns>The same application builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseScenes(this GameApplicationBuilder builder,
                                                       ISceneBackend backend)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (backend == null)
                throw new ArgumentNullException(nameof(backend));

            return builder.AddModule(new SceneModule(backend));
        }
    }
}
