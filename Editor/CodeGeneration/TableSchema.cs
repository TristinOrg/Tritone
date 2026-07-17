using System;

namespace Tritone.Editor.CodeGeneration
{
    /// <summary>
    /// Describes all strongly typed configuration tables generated for one project.
    /// </summary>
    [Serializable]
    internal sealed class TableSchema
    {
        // Stores the namespace used by generated table types.
        public string Namespace;

        // Stores the optional generated source directory.
        public string OutputPath;

        // Stores every table definition.
        public TableDefinition[] Tables;
    }

    /// <summary>
    /// Describes one generated configuration row and its asset path.
    /// </summary>
    [Serializable]
    internal sealed class TableDefinition
    {
        // Stores the table name used for generated types.
        public string Name;

        // Stores the runtime asset path.
        public string Path;

        // Stores the serialized row fields.
        public TableFieldDefinition[] Fields;
    }

    /// <summary>
    /// Describes one serializable configuration row field.
    /// </summary>
    [Serializable]
    internal sealed class TableFieldDefinition
    {
        // Stores the generated field name.
        public string Name;

        // Stores the supported C# field type.
        public string Type;

        // Indicates whether this field is the primary key.
        public bool Key;
    }
}
