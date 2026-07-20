using System;
using Tritone.Kernel;

namespace Tritone.Tweening
{
    /// <summary>
    /// Provides tween scheduler configuration for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the shared allocation-free tween scheduler.
        /// </summary>
        public static GameApplicationBuilder UseTweens(
            this GameApplicationBuilder builder,
            int capacity          = 64,
            int maxStepsPerUpdate = 4096,
            int order             = 10)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            return builder.AddModule(
                new TweenModule(capacity, maxStepsPerUpdate, order));
        }
    }
}
