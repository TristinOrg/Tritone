using System;

namespace Tritone.Editor.CodeGeneration
{
    /// <summary>
    /// Describes one generated configuration row, source file, and runtime asset path.
    /// </summary>
    [Serializable]
    public sealed class TableDefinition
    {
        /// <summary>
        /// Stores the table name used for generated types.
        /// </summary>
        public string Name;

        /// <summary>
        /// Stores the runtime asset path.
        /// </summary>
        public string Path;

        /// <summary>
        /// Stores the optional CSV, TSV, or custom source path.
        /// </summary>
        public string Source;

        /// <summary>
        /// Stores the optional generated data file name.
        /// </summary>
        public string DataFile;

        /// <summary>
        /// Stores optional explicit row fields for compatibility; source files infer fields when omitted.
        /// </summary>
        public TableFieldDefinition[] Fields;
    }
}
