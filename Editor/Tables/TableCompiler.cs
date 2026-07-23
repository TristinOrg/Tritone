using System;
using System.Collections.Generic;
using System.IO;
using Tritone.Editor.CodeGeneration;

namespace Tritone.Editor.Tables
{
    /// <summary>
    /// Validates table schemas and sources before atomically committing generated code and data.
    /// </summary>
    public sealed class TableCompiler
    {
        /// <summary>Stores source readers in deterministic resolution order.</summary>
        private readonly ITableSourceReader[] mSourceReaders;

        /// <summary>Stores explicitly registered field types.</summary>
        private readonly Dictionary<string, ITableFieldType> mFieldTypes;

        /// <summary>Stores project validators in deterministic execution order.</summary>
        private readonly ITableValidator[] mValidators;

        /// <summary>Stores the generated code strategy.</summary>
        private readonly ITableCodeGenerator mCodeGenerator;

        /// <summary>Stores the generated data strategy.</summary>
        private readonly ITableDataWriter mDataWriter;

        /// <summary>Creates one immutable table compiler.</summary>
        /// <param name="sourceReaders">The ordered source readers.</param>
        /// <param name="fieldTypes">The registered field types.</param>
        /// <param name="validators">The ordered validators.</param>
        /// <param name="codeGenerator">The code generator.</param>
        /// <param name="dataWriter">The data writer.</param>
        internal TableCompiler(ITableSourceReader[] sourceReaders, Dictionary<string, ITableFieldType> fieldTypes, ITableValidator[] validators, ITableCodeGenerator codeGenerator, ITableDataWriter dataWriter)
        {
            mSourceReaders = sourceReaders;
            mFieldTypes    = fieldTypes;
            mValidators    = validators;
            mCodeGenerator = codeGenerator;
            mDataWriter    = dataWriter;
        }

        /// <summary>Creates the standard compiler used by the Tritone Unity menu.</summary>
        /// <returns>The standard compiler.</returns>
        public static TableCompiler CreateDefault()
        {
            return new TableCompilerBuilder()
                .AddSourceReader(new DelimitedTableSourceReader())
                .AddDefaultFieldTypes()
                .UseCodeGenerator(new CSharpTableCodeGenerator())
                .UseDataWriter(new JsonTableDataWriter())
                .Build();
        }

        /// <summary>Compiles one complete schema and commits no files when validation fails.</summary>
        /// <param name="schema">The schema to compile.</param>
        /// <returns>The structured build result.</returns>
        public TableBuildResult Compile(TableSchema schema)
        {
            var diagnostics = new TableDiagnosticCollection();
            var outputs     = new List<TableGeneratedFile>();
            var tables      = TableDirectoryDiscovery.Discover(schema, diagnostics);
            ValidateSchema(schema, tables, diagnostics);
            if (diagnostics.HasErrors)
            {
                return new TableBuildResult(false, false, diagnostics.ToArray());
            }

            var tableNames = new HashSet<string>(StringComparer.Ordinal);
            var paths      = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in tables)
            {
                var source     = ReadSource(table, diagnostics);
                var definition = InferDefinition(table, source, diagnostics, out source);
                if (!ValidateDefinition(definition, tableNames, diagnostics, out var fieldTypes))
                {
                    continue;
                }
                AddOutput(mCodeGenerator.Generate(schema, definition, fieldTypes), paths, outputs, diagnostics);
                if (source == null)
                {
                    continue;
                }
                var compilation = CompileSource(definition, source, fieldTypes, diagnostics);
                if (compilation == null)
                {
                    continue;
                }
                foreach (var validator in mValidators)
                {
                    validator.Validate(compilation, diagnostics);
                }
                if (!diagnostics.HasErrors)
                {
                    AddOutput(mDataWriter.Generate(schema, compilation), paths, outputs, diagnostics);
                }
            }
            if (diagnostics.HasErrors)
            {
                return new TableBuildResult(false, false, diagnostics.ToArray());
            }
            try
            {
                var changed = TableOutputTransaction.Commit(outputs);
                return new TableBuildResult(true, changed, diagnostics.ToArray());
            }
            catch (Exception exception)
            {
                diagnostics.Error("TRT-TABLE-5001", $"Generated output transaction failed: {exception.Message}", new TableSourceLocation(null, 0, 0));
                return new TableBuildResult(false, false, diagnostics.ToArray());
            }
        }

        /// <summary>Reads a configured source once before schema resolution.</summary>
        /// <param name="table">The table definition.</param>
        /// <param name="diagnostics">The diagnostic destination.</param>
        /// <returns>The source data, or null when no source is configured or reading fails.</returns>
        private TableSourceData ReadSource(TableDefinition table, TableDiagnosticCollection diagnostics)
        {
            if (table == null || string.IsNullOrWhiteSpace(table.Source))
            {
                return null;
            }
            var reader = GetReader(table.Source);
            if (reader == null)
            {
                diagnostics.Error("TRT-TABLE-3001", $"No table source reader supports '{table.Source}'.", new TableSourceLocation(table.Source, 0, 0));
                return null;
            }
            return reader.Read(table.Source, diagnostics);
        }

        /// <summary>Infers fields from the source type row when no explicit fields are configured.</summary>
        /// <param name="table">The configured table.</param>
        /// <param name="source">The raw source data.</param>
        /// <param name="diagnostics">The diagnostic destination.</param>
        /// <param name="dataSource">The source containing data rows only.</param>
        /// <returns>The original or inferred table definition.</returns>
        private static TableDefinition InferDefinition(TableDefinition table, TableSourceData source, TableDiagnosticCollection diagnostics, out TableSourceData dataSource)
        {
            dataSource = source;
            if (table == null || table.Fields != null && table.Fields.Length > 0)
            {
                return table;
            }
            if (source == null || source.Rows.Length == 0)
            {
                diagnostics.Error("TRT-TABLE-2105", $"Table '{table?.Name}' requires a type row or explicit fields.", new TableSourceLocation(table?.Source, 2, 1));
                return table;
            }
            var typeRow = source.Rows[0];
            if (typeRow.Cells.Length != source.Headers.Length)
            {
                diagnostics.Error("TRT-TABLE-3004", "The type row must contain one type for every header.", new TableSourceLocation(source.Source, typeRow.Number, 1));
                return table;
            }
            var fields = new TableFieldDefinition[source.Headers.Length];
            for (var i = 0; i < fields.Length; i++)
            {
                fields[i] = new TableFieldDefinition
                {
                    Name = source.Headers[i]?.Trim(),
                    Type = typeRow.Cells[i]?.Trim(),
                    Key  = i == 0
                };
            }
            var rows = new TableSourceRow[source.Rows.Length - 1];
            Array.Copy(source.Rows, 1, rows, 0, rows.Length);
            dataSource = new TableSourceData(source.Source, source.Headers, rows);
            return new TableDefinition
            {
                Name     = table.Name,
                Path     = table.Path,
                Source   = table.Source,
                DataFile = table.DataFile,
                Fields   = fields
            };
        }

        /// <summary>Validates the schema root shared by every table.</summary>
        /// <param name="schema">The schema root.</param>
        /// <param name="tables">The resolved explicit and discovered tables.</param>
        /// <param name="diagnostics">The diagnostic destination.</param>
        private static void ValidateSchema(TableSchema schema, TableDefinition[] tables, TableDiagnosticCollection diagnostics)
        {
            if (schema == null)
            {
                diagnostics.Error("TRT-TABLE-2001", "Table schema is null.", new TableSourceLocation(null, 0, 0));
                return;
            }
            try
            {
                GenerationUtility.ValidateNamespace(schema.Namespace, "Table namespace");
            }
            catch (Exception exception)
            {
                diagnostics.Error("TRT-TABLE-2002", exception.Message, new TableSourceLocation(null, 0, 0));
            }
            if (tables.Length == 0)
            {
                diagnostics.Error("TRT-TABLE-2003", "At least one table definition is required.", new TableSourceLocation(null, 0, 0));
            }
        }

        /// <summary>Validates one definition and resolves its field types.</summary>
        /// <param name="table">The table definition.</param>
        /// <param name="tableNames">The names already encountered.</param>
        /// <param name="diagnostics">The diagnostic destination.</param>
        /// <param name="fieldTypes">The resolved field types.</param>
        /// <returns>True when code generation can continue.</returns>
        private bool ValidateDefinition(TableDefinition table, HashSet<string> tableNames, TableDiagnosticCollection diagnostics, out ITableFieldType[] fieldTypes)
        {
            fieldTypes = null;
            if (table == null)
            {
                diagnostics.Error("TRT-TABLE-2101", "Table definition is null.", new TableSourceLocation(null, 0, 0));
                return false;
            }
            try
            {
                GenerationUtility.ValidateIdentifier(table.Name, "Table name");
            }
            catch (Exception exception)
            {
                diagnostics.Error("TRT-TABLE-2102", exception.Message, new TableSourceLocation(table.Source, 0, 0));
                return false;
            }
            if (!tableNames.Add(table.Name))
            {
                diagnostics.Error("TRT-TABLE-2103", $"Table '{table.Name}' is defined more than once.", new TableSourceLocation(table.Source, 0, 0));
            }
            if (string.IsNullOrWhiteSpace(table.Path))
            {
                diagnostics.Error("TRT-TABLE-2104", $"Table '{table.Name}' requires a runtime asset path.", new TableSourceLocation(table.Source, 0, 0));
            }
            if (table.Fields == null || table.Fields.Length == 0)
            {
                diagnostics.Error("TRT-TABLE-2105", $"Table '{table.Name}' requires at least one field.", new TableSourceLocation(table.Source, 0, 0));
                return false;
            }

            fieldTypes     = new ITableFieldType[table.Fields.Length];
            var fieldNames = new HashSet<string>(StringComparer.Ordinal);
            var keyCount   = 0;
            for (var i = 0; i < table.Fields.Length; i++)
            {
                var field = table.Fields[i];
                if (field == null)
                {
                    diagnostics.Error("TRT-TABLE-2201", $"Table '{table.Name}' field {i} is null.", new TableSourceLocation(table.Source, 0, i + 1));
                    continue;
                }
                try
                {
                    GenerationUtility.ValidateIdentifier(field.Name, "Table field");
                }
                catch (Exception exception)
                {
                    diagnostics.Error("TRT-TABLE-2202", exception.Message, new TableSourceLocation(table.Source, 0, i + 1));
                }
                if (!fieldNames.Add(field.Name))
                {
                    diagnostics.Error("TRT-TABLE-2203", $"Table '{table.Name}' field '{field.Name}' is duplicated.", new TableSourceLocation(table.Source, 0, i + 1));
                }
                if (!mFieldTypes.TryGetValue(field.Type, out fieldTypes[i]))
                {
                    diagnostics.Error("TRT-TABLE-2204", $"Table '{table.Name}' field '{field.Name}' uses unregistered type '{field.Type}'.", new TableSourceLocation(table.Source, 0, i + 1));
                }
                if (field.Key)
                {
                    keyCount++;
                }
            }
            if (keyCount != 1)
            {
                diagnostics.Error("TRT-TABLE-2205", $"Table '{table.Name}' must define exactly one key field.", new TableSourceLocation(table.Source, 0, 0));
            }
            return !diagnostics.HasErrors;
        }

        /// <summary>Reads and normalizes one configured external table source.</summary>
        /// <param name="table">The table definition.</param>
        /// <param name="fieldTypes">The resolved field types.</param>
        /// <param name="diagnostics">The diagnostic destination.</param>
        /// <returns>The normalized compilation, or null after an error.</returns>
        private static TableCompilation CompileSource(TableDefinition table, TableSourceData source, ITableFieldType[] fieldTypes, TableDiagnosticCollection diagnostics)
        {
            if (source == null || diagnostics.HasErrors)
            {
                return null;
            }
            var columns = MapColumns(table, source, diagnostics);
            if (columns == null)
            {
                return null;
            }

            var values = new string[source.Rows.Length][];
            var keys   = new HashSet<string>(StringComparer.Ordinal);
            var keyIndex = GetKeyIndex(table);
            for (var rowIndex = 0; rowIndex < source.Rows.Length; rowIndex++)
            {
                var row       = source.Rows[rowIndex];
                var rowValues = new string[table.Fields.Length];
                values[rowIndex] = rowValues;
                for (var fieldIndex = 0; fieldIndex < table.Fields.Length; fieldIndex++)
                {
                    var field       = table.Fields[fieldIndex];
                    var sourceIndex = columns[fieldIndex];
                    var rawValue    = sourceIndex < row.Cells.Length ? row.Cells[sourceIndex] : null;
                    if (string.IsNullOrEmpty(rawValue))
                    {
                        rawValue = ResolveEmptyValue(field, fieldTypes[fieldIndex]);
                    }
                    if (rawValue == null || !fieldTypes[fieldIndex].TryConvert(rawValue, out rowValues[fieldIndex]))
                    {
                        diagnostics.Error("TRT-TABLE-3101", $"Value '{rawValue}' cannot be converted to '{field.Type}' for field '{field.Name}'.", new TableSourceLocation(source.Source, row.Number, sourceIndex + 1));
                    }
                }
                var key = rowValues[keyIndex];
                if (key != null && !keys.Add(key))
                {
                    diagnostics.Error("TRT-TABLE-3102", $"Table '{table.Name}' contains duplicate key {key}.", new TableSourceLocation(source.Source, row.Number, columns[keyIndex] + 1));
                }
            }
            return diagnostics.HasErrors ? null : new TableCompilation(table, source, values, fieldTypes);
        }

        /// <summary>Finds the first reader supporting one source path.</summary>
        /// <param name="path">The source path.</param>
        /// <returns>The matching reader, or null.</returns>
        private ITableSourceReader GetReader(string path)
        {
            foreach (var reader in mSourceReaders)
            {
                if (reader.CanRead(path))
                {
                    return reader;
                }
            }
            return null;
        }

        /// <summary>Maps schema fields to unique source headers.</summary>
        /// <param name="table">The table definition.</param>
        /// <param name="source">The raw source.</param>
        /// <param name="diagnostics">The diagnostic destination.</param>
        /// <returns>Source indexes in schema-field order, or null after an error.</returns>
        private static int[] MapColumns(TableDefinition table, TableSourceData source, TableDiagnosticCollection diagnostics)
        {
            var headerIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < source.Headers.Length; i++)
            {
                var header = source.Headers[i]?.Trim();
                if (string.IsNullOrEmpty(header) || !headerIndexes.TryAdd(header, i))
                {
                    diagnostics.Error("TRT-TABLE-3002", $"Source header '{header}' is empty or duplicated.", new TableSourceLocation(source.Source, 1, i + 1));
                }
            }
            var columns = new int[table.Fields.Length];
            for (var i = 0; i < table.Fields.Length; i++)
            {
                if (!headerIndexes.TryGetValue(table.Fields[i].Name, out columns[i]))
                {
                    diagnostics.Error("TRT-TABLE-3003", $"Source is missing required field '{table.Fields[i].Name}'.", new TableSourceLocation(source.Source, 1, 0));
                }
            }
            return diagnostics.HasErrors ? null : columns;
        }

        /// <summary>Resolves an empty source cell according to field optionality and type.</summary>
        /// <param name="field">The field definition.</param>
        /// <param name="fieldType">The resolved field type.</param>
        /// <returns>The raw fallback value, or null when empty is invalid.</returns>
        private static string ResolveEmptyValue(TableFieldDefinition field, ITableFieldType fieldType)
        {
            if (!string.IsNullOrEmpty(field.DefaultValue))
            {
                return field.DefaultValue;
            }
            if (!field.Optional)
            {
                return null;
            }
            if (fieldType.CSharpTypeName == "string")
            {
                return string.Empty;
            }
            return fieldType.CSharpTypeName == "bool" ? "false" : "0";
        }

        /// <summary>Gets the validated key field index.</summary>
        /// <param name="table">The table definition.</param>
        /// <returns>The key field index.</returns>
        private static int GetKeyIndex(TableDefinition table)
        {
            for (var i = 0; i < table.Fields.Length; i++)
            {
                if (table.Fields[i].Key)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Adds one output while rejecting path collisions before filesystem mutation.</summary>
        /// <param name="file">The generated output.</param>
        /// <param name="paths">The output paths already reserved.</param>
        /// <param name="outputs">The transaction outputs.</param>
        /// <param name="diagnostics">The diagnostic destination.</param>
        private static void AddOutput(TableGeneratedFile file, HashSet<string> paths, List<TableGeneratedFile> outputs, TableDiagnosticCollection diagnostics)
        {
            var fullPath = Path.GetFullPath(file.Path);
            if (!paths.Add(fullPath))
            {
                diagnostics.Error("TRT-TABLE-4001", $"Generated output path '{file.Path}' is duplicated.", new TableSourceLocation(file.Path, 0, 0));
                return;
            }
            outputs.Add(file);
        }
    }
}
