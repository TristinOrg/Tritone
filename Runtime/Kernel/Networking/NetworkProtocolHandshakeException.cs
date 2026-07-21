using System;

namespace Tritone.Networking
{
    /// <summary>
    /// Reports an explicit protocol incompatibility returned during connection validation.
    /// </summary>
    public sealed class NetworkProtocolHandshakeException : Exception
    {
        /// <summary>
        /// Gets the compatibility result that rejected the connection.
        /// </summary>
        public ENetworkProtocolCompatibility Compatibility { get; }

        /// <summary>
        /// Gets the remote protocol descriptor received from the server.
        /// </summary>
        public NetworkProtocolDescriptor RemoteProtocol { get; }

        /// <summary>
        /// Initializes one protocol handshake rejection.
        /// </summary>
        /// <param name="compatibility">The compatibility result that rejected the connection.</param>
        /// <param name="remoteProtocol">The remote protocol descriptor received from the server.</param>
        public NetworkProtocolHandshakeException(ENetworkProtocolCompatibility compatibility,
                                                 in NetworkProtocolDescriptor remoteProtocol)
            : base($"Network protocol handshake failed with compatibility result '{compatibility}'.")
        {
            Compatibility = compatibility;
            RemoteProtocol = remoteProtocol;
        }
    }
}
