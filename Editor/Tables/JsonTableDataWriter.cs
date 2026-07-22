using System.IO;
using System.Text;
using Tritone.Editor.CodeGeneration;

namespace Tritone.Editor.Tables
{
    /// <summary>
    /// Writes deterministic JSON payloads consumed by UnityJsonTableDeserializer.
    /// </summary>
    public sealed class JsonTableDataWriter : ITableDataWriter
    {
        /// <inheritdoc />
        public string Extension => "json";

        /// <inheritdoc />
        public TableGeneratedFile Generate(TableSchema schema, TableCompilation compilation)
        {
            var table      = compilation.Definition;
            var output     = string.IsNullOrWhiteSpace(schema.DataOutputPath) ? "Assets/Generated/Tritone/TableData" : schema.DataOutputPath;
            var fileName   = string.IsNullOrWhiteSpace(table.DataFile) ? table.Name + "." + Extension : table.DataFile;
            var builder    = new StringBuilder(256 + compilation.Values.Length * table.Fields.Length * 16);
            builder.Append("{\n  \"Rows\": [");
            for (var rowIndex = 0; rowIndex < compilation.Values.Length; rowIndex++)
            {
                builder.Append(rowIndex == 0 ? "\n    {" : ",\n    {");
                var values = compilation.Values[rowIndex];
                for (var fieldIndex = 0; fieldIndex < table.Fields.Length; fieldIndex++)
                {
                    builder.Append(fieldIndex == 0 ? "\n      \"" : ",\n      \"");
                    builder.Append(table.Fields[fieldIndex].Name);
                    builder.Append("\": ");
                    builder.Append(values[fieldIndex]);
                }
                builder.Append("\n    }");
            }
            if (compilation.Values.Length > 0)
            {
                builder.Append('\n');
            }
            builder.Append("  ]\n}\n");
            return new TableGeneratedFile(Path.Combine(output, fileName), builder.ToString());
        }
    }
}
