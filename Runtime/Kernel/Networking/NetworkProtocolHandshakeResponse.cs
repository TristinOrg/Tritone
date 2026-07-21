namespace Tritone.Networking
{
    /// <summary>
    /// Contains the authoritative server protocol and its compatibility decision.
    /// </summary>
    public readonly struct NetworkProtocolHandshakeResponse
    {
        /// <summary>
        /// Gets the server protocol descriptor.
        /// </summary>
        public NetworkProtocolDescriptor RemoteProtocol { get; }

        /// <summary>
        /// Gets the compatibility decision made by the server.
        /// </summary>
        public ENetworkProtocolCompatibility Compatibility { get; }

        /// <summary>
        /// Initializes one immutable protocol handshake response.
        /// </summary>
        /// <param name="remoteProtocol">The server protocol descriptor.</param>
        /// <param name="compatibility">The compatibility decision made by the server.</param>
        public NetworkProtocolHandshakeResponse(in NetworkProtocolDescriptor remoteProtocol,
                                                ENetworkProtocolCompatibility compatibility)
        {
            RemoteProtocol = remoteProtocol;
            Compatibility  = compatibility;
        }
    }
}
