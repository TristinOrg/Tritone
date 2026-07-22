using System;

namespace Tritone.Editor.CodeGeneration
{
    /// <summary>
    /// Describes one serializable configuration row field.
    /// </summary>
    [Serializable]
    public sealed class TableFieldDefinition
    {
        /// <summary>
        /// Stores the generated field name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Stores the registered field type name.
        /// </summary>
        public string Type;

        /// <summary>
        /// Stores the optional generated XML documentation.
        /// </summary>
        public string Description;

        /// <summary>
        /// Stores the raw default value used when an optional cell is empty.
        /// </summary>
        public string DefaultValue;

        /// <summary>
        /// Indicates whether this field is the primary key.
        /// </summary>
        public bool Key;

        /// <summary>
        /// Indicates whether an empty source cell is accepted.
        /// </summary>
        public bool Optional;
    }
}
