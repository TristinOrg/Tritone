using System;

namespace Tritone.Editor.CodeGeneration
{
    /// <summary>
    /// Describes all binary network messages generated for one project.
    /// </summary>
    [Serializable]
    internal sealed class NetworkSchema
    {
        /// <summary>
        /// Stores the stable protocol family identifier exchanged by peers.
        /// </summary>
        public string ProtocolId;

        /// <summary>
        /// Stores the breaking protocol version.
        /// </summary>
        public int MajorVersion;

        /// <summary>
        /// Stores the current backward-compatible protocol version.
        /// </summary>
        public int MinorVersion;

        /// <summary>
        /// Stores the oldest remote minor version accepted by this schema.
        /// </summary>
        public int MinimumMinorVersion;

        // Stores the namespace used by generated message types.
        public string Namespace;

        // Stores the optional generated source directory.
        public string OutputPath;

        // Stores every network message definition.
        public NetworkMessageDefinition[] Messages;
    }

    /// <summary>
    /// Describes one message identifier, role, relationship, and payload.
    /// </summary>
    [Serializable]
    internal sealed class NetworkMessageDefinition
    {
        // Stores the stable positive wire identifier.
        public int Id;

        // Stores the generated message type name.
        public string Name;

        // Stores Message, Request, or Response behavior.
        public string Kind;

        // Stores the response type expected by a request.
        public string Response;

        // Stores the explicitly encoded payload fields.
        public NetworkFieldDefinition[] Fields;
    }

    /// <summary>
    /// Describes one explicitly encoded network message field.
    /// </summary>
    [Serializable]
    internal sealed class NetworkFieldDefinition
    {
        // Stores the generated payload field name.
        public string Name;

        // Stores the supported binary field type.
        public string Type;
    }
}
