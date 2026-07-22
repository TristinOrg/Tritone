using System;

namespace Tritone.Editor.CodeGeneration
{
    /// <summary>
    /// Describes all strongly typed configuration tables generated for one project.
    /// </summary>
    [Serializable]
    public sealed class TableSchema
    {
        /// <summary>
        /// Stores the namespace used by generated table types.
        /// </summary>
        public string Namespace;

        /// <summary>
        /// Stores the optional generated source directory.
        /// </summary>
        public string OutputPath;

        /// <summary>
        /// Stores the optional generated table-data directory.
        /// </summary>
        public string DataOutputPath;

        /// <summary>
        /// Stores every table definition.
        /// </summary>
        public TableDefinition[] Tables;
    }
}
