using System;
using System.IO;
using Tritone.Kernel;
using Tritone.Saves;
using UnityEngine;

namespace Tritone.Unity.Saves
{
    /// <summary>
    /// Provides local save setup for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds JSON saves beneath the application's persistent data path.
        /// </summary>
        /// <param name="builder">The application builder receiving save infrastructure.</param>
        /// <returns>The same application builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseSaves(this GameApplicationBuilder builder)
        {
            return UseSaves(builder,
                            Path.Combine(Application.persistentDataPath, "Saves"),
                            new UnityJsonSaveSerializer());
        }

        /// <summary>
        /// Adds saves with replaceable storage location and serialization.
        /// </summary>
        /// <param name="builder">The application builder receiving save infrastructure.</param>
        /// <param name="rootPath">The local save root path.</param>
        /// <param name="serializer">The save data serializer.</param>
        /// <returns>The same application builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseSaves(this GameApplicationBuilder builder,
                                                      string rootPath,
                                                      ISaveSerializer serializer)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            return builder.AddModule(new SaveModule(rootPath, serializer));
        }
    }
}
