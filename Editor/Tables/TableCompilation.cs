using Tritone.Editor.CodeGeneration;

namespace Tritone.Editor.Tables
{
    /// <summary>
    /// Stores one validated source with normalized JSON values and resolved field types.
    /// </summary>
    public sealed class TableCompilation
    {
        /// <summary>Gets the owning table definition.</summary>
        public TableDefinition Definition { get; }

        /// <summary>Gets the original source.</summary>
        public TableSourceData Source { get; }

        /// <summary>Gets normalized JSON values in row and field order.</summary>
        public string[][] Values { get; }

        /// <summary>Gets field types in schema-field order.</summary>
        public ITableFieldType[] FieldTypes { get; }

        /// <summary>Creates one immutable validated compilation.</summary>
        /// <param name="definition">The table definition.</param>
        /// <param name="source">The original source.</param>
        /// <param name="values">The normalized row values.</param>
        /// <param name="fieldTypes">The resolved field types.</param>
        public TableCompilation(TableDefinition definition, TableSourceData source, string[][] values, ITableFieldType[] fieldTypes)
        {
            Definition = definition;
            Source      = source;
            Values      = values;
            FieldTypes  = fieldTypes;
        }
    }
}
