using System;
using Tritone.Kernel;

namespace Tritone.Dispatching
{
    /// <summary>
    /// Provides concise main-thread dispatcher setup for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the bounded cross-thread callback dispatcher to the application.
        /// </summary>
        /// <param name="builder">The application builder receiving dispatch infrastructure.</param>
        /// <param name="maxCallbacksPerUpdate">The callback safety limit for one application frame.</param>
        /// <param name="order">The execution order in the pre-update stage.</param>
        /// <returns>The supplied builder so additional configuration can be chained.</returns>
        public static GameApplicationBuilder UseMainThreadDispatcher(this GameApplicationBuilder builder,
                                                                     int maxCallbacksPerUpdate = 4096,
                                                                     int order = -10000)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            return builder.AddModule(new MainThreadDispatcherModule(maxCallbacksPerUpdate, order));
        }
    }
}
