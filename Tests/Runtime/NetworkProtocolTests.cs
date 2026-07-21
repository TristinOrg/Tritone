using NUnit.Framework;
using Tritone.Networking;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies deterministic network protocol version compatibility decisions.
    /// </summary>
    public sealed class NetworkProtocolTests
    {
        /// <summary>
        /// Verifies that overlapping minor version ranges are compatible in both directions.
        /// </summary>
        [Test]
        public void EvaluateCompatibility_WithOverlappingRangesReturnsCompatible()
        {
            var older = new NetworkProtocolDescriptor("game", 1, 3, 1, "older-schema");
            var newer = new NetworkProtocolDescriptor("game", 1, 5, 3, "newer-schema");

            Assert.AreEqual(ENetworkProtocolCompatibility.Compatible, older.EvaluateCompatibility(in newer));
            Assert.AreEqual(ENetworkProtocolCompatibility.Compatible, newer.EvaluateCompatibility(in older));
        }

        /// <summary>
        /// Verifies that equal semantic versions cannot hide different generated schemas.
        /// </summary>
        [Test]
        public void EvaluateCompatibility_WithSchemaDriftReturnsSchemaMismatch()
        {
            var local = new NetworkProtocolDescriptor("game", 1, 3, 1, "schema-a");
            var remote = new NetworkProtocolDescriptor("game", 1, 3, 1, "schema-b");

            Assert.AreEqual(ENetworkProtocolCompatibility.SchemaMismatch, local.EvaluateCompatibility(in remote));
        }

        /// <summary>
        /// Verifies specific diagnostics for protocol, major, and minor incompatibilities.
        /// </summary>
        [Test]
        public void EvaluateCompatibility_WithIncompatibleVersionsReturnsExactReason()
        {
            var local = new NetworkProtocolDescriptor("game", 2, 4, 2, "schema");
            var otherProtocol = new NetworkProtocolDescriptor("tools", 2, 4, 2, "schema");
            var otherMajor = new NetworkProtocolDescriptor("game", 3, 4, 2, "schema");
            var olderRemote = new NetworkProtocolDescriptor("game", 2, 1, 0, "schema");
            var newerRemote = new NetworkProtocolDescriptor("game", 2, 6, 5, "schema");

            Assert.AreEqual(ENetworkProtocolCompatibility.ProtocolMismatch, local.EvaluateCompatibility(in otherProtocol));
            Assert.AreEqual(ENetworkProtocolCompatibility.MajorVersionMismatch, local.EvaluateCompatibility(in otherMajor));
            Assert.AreEqual(ENetworkProtocolCompatibility.RemoteVersionTooOld, local.EvaluateCompatibility(in olderRemote));
            Assert.AreEqual(ENetworkProtocolCompatibility.LocalVersionTooOld, local.EvaluateCompatibility(in newerRemote));
        }

        /// <summary>
        /// Verifies that a default value-type descriptor cannot accidentally pass a handshake.
        /// </summary>
        [Test]
        public void EvaluateCompatibility_WithDefaultDescriptorReturnsInvalidDescriptor()
        {
            var local = new NetworkProtocolDescriptor("game", 1, 0, 0, "schema");
            var remote = default(NetworkProtocolDescriptor);

            Assert.AreEqual(ENetworkProtocolCompatibility.InvalidDescriptor, local.EvaluateCompatibility(in remote));
        }
    }
}
