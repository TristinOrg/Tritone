namespace Tritone.Networking
{
    /// <summary>
    /// Describes why two generated network protocol versions can or cannot communicate.
    /// </summary>
    public enum ENetworkProtocolCompatibility
    {
        /// <summary>
        /// Indicates that at least one descriptor was not constructed with valid protocol data.
        /// </summary>
        InvalidDescriptor,

        /// <summary>
        /// Indicates that both protocol version ranges overlap.
        /// </summary>
        Compatible,

        /// <summary>
        /// Indicates that the peers identify different protocols.
        /// </summary>
        ProtocolMismatch,

        /// <summary>
        /// Indicates that the peers use incompatible major versions.
        /// </summary>
        MajorVersionMismatch,

        /// <summary>
        /// Indicates that the remote minor version predates the local compatibility range.
        /// </summary>
        RemoteVersionTooOld,

        /// <summary>
        /// Indicates that the local minor version predates the remote compatibility range.
        /// </summary>
        LocalVersionTooOld,

        /// <summary>
        /// Indicates that equal version numbers describe different wire schemas.
        /// </summary>
        SchemaMismatch
    }
}
