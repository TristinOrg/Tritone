using System;
using Tritone.Kernel;
using Tritone.Tables;
using Tritone.Unity.Assets;

namespace Tritone.Unity.Tables
{
    /// <summary>
    /// Provides shared configuration table setup for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds configuration tables backed by Unity JSON assets.
        /// </summary>
        /// <param name="builder">The application builder receiving table infrastructure.</param>
        /// <returns>The same application builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseTables(this GameApplicationBuilder builder)
        {
            return UseTables(builder, new UnityJsonTableDeserializer());
        }

        /// <summary>
        /// Adds configuration tables backed by a replaceable deserializer.
        /// </summary>
        /// <param name="builder">The application builder receiving table infrastructure.</param>
        /// <param name="deserializer">The deserializer converting assets into row arrays.</param>
        /// <returns>The same application builder for fluent configuration.</returns>
        public static GameApplicationBuilder UseTables(this GameApplicationBuilder builder,
                                                       ITableDeserializer deserializer)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (deserializer == null)
                throw new ArgumentNullException(nameof(deserializer));

            return builder.AddModule(new TableModule(deserializer), typeof(AssetModule));
        }
    }
}
