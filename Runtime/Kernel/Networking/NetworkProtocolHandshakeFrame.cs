using System;
using System.IO;
using Tritone.Messaging;

namespace Tritone.Networking
{
    /// <summary>
    /// Encodes and decodes framework-owned protocol handshake control frames.
    /// </summary>
    public static class NetworkProtocolHandshakeFrame
    {
        /// <summary>
        /// Stores the little-endian ASCII control frame signature TRT1.
        /// </summary>
        private const int Magic = 0x31545254;

        /// <summary>
        /// Stores the client hello control frame kind.
        /// </summary>
        private const int HelloKind = 1;

        /// <summary>
        /// Stores the server response control frame kind.
        /// </summary>
        private const int ResponseKind = 2;

        /// <summary>
        /// Determines whether a frame belongs to the Tritone protocol handshake channel.
        /// </summary>
        /// <param name="frame">The received transport frame.</param>
        /// <returns>True when the frame starts with the handshake signature.</returns>
        public static bool IsFrame(byte[] frame)
        {
            return frame != null &&
                   frame.Length >= 4 &&
                   frame[0] == (byte)(Magic & 0xFF) &&
                   frame[1] == (byte)(Magic >> 8 & 0xFF) &&
                   frame[2] == (byte)(Magic >> 16 & 0xFF) &&
                   frame[3] == (byte)(Magic >> 24 & 0xFF);
        }

        /// <summary>
        /// Creates a client hello frame containing its generated protocol descriptor.
        /// </summary>
        /// <param name="protocol">The client protocol descriptor.</param>
        /// <returns>The encoded transport control frame.</returns>
        public static byte[] CreateHello(in NetworkProtocolDescriptor protocol)
        {
            if (!protocol.IsValid)
                throw new ArgumentException("A valid client protocol descriptor is required.", nameof(protocol));

            var writer = new MessageWriter();
            writer.WriteInt32(Magic);
            writer.WriteInt32(HelloKind);
            WriteProtocol(writer, in protocol);
            return writer.ToArray();
        }

        /// <summary>
        /// Tries to decode a client hello frame on a server or test peer.
        /// </summary>
        /// <param name="frame">The received transport control frame.</param>
        /// <param name="protocol">The decoded client protocol descriptor.</param>
        /// <returns>True when the complete frame is a valid client hello.</returns>
        public static bool TryReadHello(byte[] frame, out NetworkProtocolDescriptor protocol)
        {
            return TryReadProtocolFrame(frame, HelloKind, out protocol, out _);
        }

        /// <summary>
        /// Creates an authoritative server response for one client protocol.
        /// </summary>
        /// <param name="serverProtocol">The server protocol descriptor.</param>
        /// <param name="clientProtocol">The decoded client protocol descriptor.</param>
        /// <returns>The encoded server response control frame.</returns>
        public static byte[] CreateResponse(in NetworkProtocolDescriptor serverProtocol,
                                            in NetworkProtocolDescriptor clientProtocol)
        {
            if (!serverProtocol.IsValid)
                throw new ArgumentException("A valid server protocol descriptor is required.", nameof(serverProtocol));

            var compatibility = serverProtocol.EvaluateCompatibility(in clientProtocol);
            var writer = new MessageWriter();
            writer.WriteInt32(Magic);
            writer.WriteInt32(ResponseKind);
            writer.WriteInt32((int)compatibility);
            WriteProtocol(writer, in serverProtocol);
            return writer.ToArray();
        }

        /// <summary>
        /// Tries to decode an authoritative server protocol response.
        /// </summary>
        /// <param name="frame">The received transport control frame.</param>
        /// <param name="response">The decoded server protocol and compatibility decision.</param>
        /// <returns>True when the complete frame is a valid server response.</returns>
        public static bool TryReadResponse(byte[] frame, out NetworkProtocolHandshakeResponse response)
        {
            if (TryReadProtocolFrame(frame, ResponseKind, out var protocol, out var compatibility))
            {
                response = new NetworkProtocolHandshakeResponse(in protocol, compatibility);
                return true;
            }

            response = default;
            return false;
        }

        /// <summary>
        /// Tries to decode one complete handshake frame without exposing malformed input exceptions.
        /// </summary>
        /// <param name="frame">The received transport control frame.</param>
        /// <param name="expectedKind">The expected control frame kind.</param>
        /// <param name="protocol">The decoded protocol descriptor.</param>
        /// <param name="compatibility">The decoded compatibility value for response frames.</param>
        /// <returns>True when every field is valid and no trailing data remains.</returns>
        private static bool TryReadProtocolFrame(byte[] frame,
                                                 int expectedKind,
                                                 out NetworkProtocolDescriptor protocol,
                                                 out ENetworkProtocolCompatibility compatibility)
        {
            protocol      = default;
            compatibility = ENetworkProtocolCompatibility.InvalidDescriptor;
            if (!IsFrame(frame))
                return false;

            try
            {
                var reader = new MessageReader(frame);
                if (reader.ReadInt32() != Magic || reader.ReadInt32() != expectedKind)
                    return false;
                if (expectedKind == ResponseKind)
                {
                    var value = reader.ReadInt32();
                    if (value < 0 || value > (int)ENetworkProtocolCompatibility.SchemaMismatch)
                        return false;
                    compatibility = (ENetworkProtocolCompatibility)value;
                }

                protocol = ReadProtocol(reader);
                return reader.Remaining == 0;
            }
            catch (Exception exception) when (exception is ArgumentException or IOException)
            {
                protocol = default;
                return false;
            }
        }

        /// <summary>
        /// Writes one protocol descriptor in a stable field order.
        /// </summary>
        /// <param name="writer">The target control frame writer.</param>
        /// <param name="protocol">The protocol descriptor to encode.</param>
        private static void WriteProtocol(MessageWriter writer, in NetworkProtocolDescriptor protocol)
        {
            writer.WriteString(protocol.ProtocolId);
            writer.WriteInt32(protocol.MajorVersion);
            writer.WriteInt32(protocol.MinorVersion);
            writer.WriteInt32(protocol.MinimumMinorVersion);
            writer.WriteString(protocol.SchemaFingerprint);
        }

        /// <summary>
        /// Reads and validates one protocol descriptor.
        /// </summary>
        /// <param name="reader">The source control frame reader.</param>
        /// <returns>The validated protocol descriptor.</returns>
        private static NetworkProtocolDescriptor ReadProtocol(MessageReader reader)
        {
            return new NetworkProtocolDescriptor(reader.ReadString(),
                                                 reader.ReadInt32(),
                                                 reader.ReadInt32(),
                                                 reader.ReadInt32(),
                                                 reader.ReadString());
        }
    }
}
