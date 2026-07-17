using System;
using Tritone.Kernel;

namespace Tritone.Unity.Input
{
    /// <summary>
    /// Provides input setup for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        public static GameApplicationBuilder UseInput(this GameApplicationBuilder builder)
        {
            return UseInput(builder, new UnityInputSource());
        }

        public static GameApplicationBuilder UseInput(this GameApplicationBuilder builder,
                                                      IInputSource source)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            return builder.AddModule(new InputModule(source));
        }
    }
}
