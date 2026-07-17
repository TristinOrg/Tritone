using System;
using Tritone.Kernel;
using Tritone.Messaging;

namespace Tritone.Networking
{
    /// <summary>
    /// Provides concise networking setup for an application builder.
    /// </summary>
    public static class GameApplicationBuilderExtensions
    {
        public static GameApplicationBuilder UseTcpNetwork(this GameApplicationBuilder builder,
                                                           IMessageSerializer serializer,
                                                           int maximumFrameSize = 4 * 1024 * 1024,
                                                           NetworkSessionOptions options = null)
        {
            return UseNetwork(builder,
                              serializer,
                              new TcpNetworkTransport(maximumFrameSize),
                              options);
        }

        public static GameApplicationBuilder UseNetwork(this GameApplicationBuilder builder,
                                                        IMessageSerializer serializer,
                                                        INetworkTransport transport,
                                                        NetworkSessionOptions options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            return builder.AddModule(new NetworkModule(serializer, transport, options));
        }
    }
}
