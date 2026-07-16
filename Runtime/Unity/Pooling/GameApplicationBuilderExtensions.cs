using System;
using Tritone.Kernel;

namespace Tritone.Unity.Pooling
{
    /// <summary>
    /// Provides shared pool configuration for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds lazy plain-object and Unity-prefab pools to the application.
        /// </summary>
        public static GameApplicationBuilder UsePools(this GameApplicationBuilder builder,
                                                      int defaultCapacity    = 8,
                                                      int maxCachedPerPool   = 128)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.AddModule(new PoolModule(defaultCapacity, maxCachedPerPool));
        }
    }
}
