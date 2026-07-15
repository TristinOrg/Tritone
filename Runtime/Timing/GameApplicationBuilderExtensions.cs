using System;
using Tritone.Kernel;

namespace Tritone.Timing
{
    /// <summary>
    /// Provides timer infrastructure configuration for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the shared high-performance timer scheduler to the application.
        /// </summary>
        /// <param name="builder">The application builder receiving timer infrastructure.</param>
        /// <param name="capacity">The initial capacity of each clock heap.</param>
        /// <param name="maxCallbacksPerUpdate">The callback safety limit for one update.</param>
        /// <param name="order">The execution order in the normal update stage.</param>
        /// <returns>The supplied builder so additional configuration can be chained.</returns>
        public static GameApplicationBuilder UseTimers(this GameApplicationBuilder builder,
                                                       int capacity              = 64,
                                                       int maxCallbacksPerUpdate = 4096,
                                                       int order                 = 0)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.AddModule(new TimerModule(capacity, maxCallbacksPerUpdate, order));
        }
    }
}
