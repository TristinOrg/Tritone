using System;

namespace Tritone.Networking
{
    /// <summary>
    /// Identifies one generated wire schema and the minor versions it accepts.
    /// </summary>
    public readonly struct NetworkProtocolDescriptor : IEquatable<NetworkProtocolDescriptor>
    {
        /// <summary>
        /// Gets the stable protocol family identifier.
        /// </summary>
        public string ProtocolId { get; }

        /// <summary>
        /// Gets the breaking protocol version.
        /// </summary>
        public int MajorVersion { get; }

        /// <summary>
        /// Gets the current backward-compatible protocol version.
        /// </summary>
        public int MinorVersion { get; }

        /// <summary>
        /// Gets the oldest remote minor version accepted by this build.
        /// </summary>
        public int MinimumMinorVersion { get; }

        /// <summary>
        /// Gets the deterministic generated wire schema fingerprint.
        /// </summary>
        public string SchemaFingerprint { get; }

        /// <summary>
        /// Gets whether this descriptor contains a complete valid protocol identity and version range.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(ProtocolId) &&
                               MajorVersion >= 1 &&
                               MinorVersion >= 0 &&
                               MinimumMinorVersion >= 0 &&
                               MinimumMinorVersion <= MinorVersion &&
                               !string.IsNullOrWhiteSpace(SchemaFingerprint);

        /// <summary>
        /// Initializes one immutable generated protocol descriptor.
        /// </summary>
        /// <param name="protocolId">The stable protocol family identifier.</param>
        /// <param name="majorVersion">The breaking protocol version.</param>
        /// <param name="minorVersion">The current backward-compatible protocol version.</param>
        /// <param name="minimumMinorVersion">The oldest remote minor version accepted by this build.</param>
        /// <param name="schemaFingerprint">The deterministic generated wire schema fingerprint.</param>
        public NetworkProtocolDescriptor(string protocolId,
                                         int majorVersion,
                                         int minorVersion,
                                         int minimumMinorVersion,
                                         string schemaFingerprint)
        {
            if (string.IsNullOrWhiteSpace(protocolId))
                throw new ArgumentException("A network protocol identifier is required.", nameof(protocolId));
            if (majorVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(majorVersion));
            if (minorVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(minorVersion));
            if (minimumMinorVersion < 0 || minimumMinorVersion > minorVersion)
                throw new ArgumentOutOfRangeException(nameof(minimumMinorVersion));
            if (string.IsNullOrWhiteSpace(schemaFingerprint))
                throw new ArgumentException("A network schema fingerprint is required.", nameof(schemaFingerprint));

            ProtocolId          = protocolId;
            MajorVersion        = majorVersion;
            MinorVersion        = minorVersion;
            MinimumMinorVersion = minimumMinorVersion;
            SchemaFingerprint   = schemaFingerprint;
        }

        /// <summary>
        /// Evaluates whether this local protocol can communicate with one remote protocol.
        /// </summary>
        /// <param name="remote">The remote peer protocol descriptor.</param>
        /// <returns>The exact compatibility result.</returns>
        public ENetworkProtocolCompatibility EvaluateCompatibility(in NetworkProtocolDescriptor remote)
        {
            if (!IsValid || !remote.IsValid)
                return ENetworkProtocolCompatibility.InvalidDescriptor;
            if (!string.Equals(ProtocolId, remote.ProtocolId, StringComparison.Ordinal))
                return ENetworkProtocolCompatibility.ProtocolMismatch;
            if (MajorVersion != remote.MajorVersion)
                return ENetworkProtocolCompatibility.MajorVersionMismatch;
            if (remote.MinorVersion < MinimumMinorVersion)
                return ENetworkProtocolCompatibility.RemoteVersionTooOld;
            if (MinorVersion < remote.MinimumMinorVersion)
                return ENetworkProtocolCompatibility.LocalVersionTooOld;
            if (MinorVersion == remote.MinorVersion && !string.Equals(SchemaFingerprint, remote.SchemaFingerprint, StringComparison.Ordinal))
                return ENetworkProtocolCompatibility.SchemaMismatch;
            return ENetworkProtocolCompatibility.Compatible;
        }

        /// <summary>
        /// Determines whether every descriptor value exactly matches another protocol.
        /// </summary>
        /// <param name="other">The other protocol descriptor.</param>
        /// <returns>True when every descriptor value matches.</returns>
        public bool Equals(NetworkProtocolDescriptor other)
        {
            return string.Equals(ProtocolId, other.ProtocolId, StringComparison.Ordinal) &&
                   MajorVersion == other.MajorVersion &&
                   MinorVersion == other.MinorVersion &&
                   MinimumMinorVersion == other.MinimumMinorVersion &&
                   string.Equals(SchemaFingerprint, other.SchemaFingerprint, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is NetworkProtocolDescriptor other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(ProtocolId ?? string.Empty);
                hash = hash * 31 + MajorVersion;
                hash = hash * 31 + MinorVersion;
                hash = hash * 31 + MinimumMinorVersion;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(SchemaFingerprint ?? string.Empty);
                return hash;
            }
        }

        /// <summary>
        /// Determines whether two protocol descriptors exactly match.
        /// </summary>
        /// <param name="left">The left protocol descriptor.</param>
        /// <param name="right">The right protocol descriptor.</param>
        /// <returns>True when every descriptor value matches.</returns>
        public static bool operator ==(NetworkProtocolDescriptor left, NetworkProtocolDescriptor right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two protocol descriptors differ.
        /// </summary>
        /// <param name="left">The left protocol descriptor.</param>
        /// <param name="right">The right protocol descriptor.</param>
        /// <returns>True when at least one descriptor value differs.</returns>
        public static bool operator !=(NetworkProtocolDescriptor left, NetworkProtocolDescriptor right)
        {
            return !left.Equals(right);
        }
    }
}
