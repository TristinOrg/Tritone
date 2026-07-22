using Tritone.Editor.CodeGeneration;

namespace Tritone.Editor.Tables
{
    /// <summary>
    /// Defines the severity of one table compiler diagnostic.
    /// </summary>
    public enum ETableDiagnosticSeverity
    {
        /// <summary>Describes non-blocking build information.</summary>
        Info,

        /// <summary>Describes a suspicious but buildable value.</summary>
        Warning,

        /// <summary>Describes an error that prevents every output from being committed.</summary>
        Error
    }

    /// <summary>
    /// Identifies one table source position.
    /// </summary>
    public readonly struct TableSourceLocation
    {
        /// <summary>Stores the source path.</summary>
        public readonly string Source;

        /// <summary>Stores the one-based row number.</summary>
        public readonly int Row;

        /// <summary>Stores the one-based column number.</summary>
        public readonly int Column;

        /// <summary>Creates one immutable source position.</summary>
        /// <param name="source">The source path.</param>
        /// <param name="row">The one-based row number.</param>
        /// <param name="column">The one-based column number.</param>
        public TableSourceLocation(string source, int row, int column)
        {
            Source = source;
            Row    = row;
            Column = column;
        }
    }

    /// <summary>
    /// Describes one structured table build diagnostic.
    /// </summary>
    public readonly struct TableDiagnostic
    {
        /// <summary>Stores the diagnostic severity.</summary>
        public readonly ETableDiagnosticSeverity Severity;

        /// <summary>Stores the stable machine-readable diagnostic code.</summary>
        public readonly string Code;

        /// <summary>Stores the developer-facing diagnostic message.</summary>
        public readonly string Message;

        /// <summary>Stores the source position associated with the diagnostic.</summary>
        public readonly TableSourceLocation Location;

        /// <summary>Creates one immutable diagnostic.</summary>
        /// <param name="severity">The diagnostic severity.</param>
        /// <param name="code">The stable diagnostic code.</param>
        /// <param name="message">The diagnostic message.</param>
        /// <param name="location">The source position.</param>
        public TableDiagnostic(ETableDiagnosticSeverity severity, string code, string message, TableSourceLocation location)
        {
            Severity = severity;
            Code     = code;
            Message  = message;
            Location = location;
        }
    }

    /// <summary>
    /// Stores one source row without interpreting its field values.
    /// </summary>
    public readonly struct TableSourceRow
    {
        /// <summary>Stores the one-based source row number.</summary>
        public readonly int Number;

        /// <summary>Stores raw cells in source-column order.</summary>
        public readonly string[] Cells;

        /// <summary>Creates one immutable source row.</summary>
        /// <param name="number">The one-based source row number.</param>
        /// <param name="cells">The raw source cells.</param>
        public TableSourceRow(int number, string[] cells)
        {
            Number = number;
            Cells  = cells;
        }
    }

    /// <summary>
    /// Stores one generated output entirely in memory before transaction commit.
    /// </summary>
    public readonly struct TableGeneratedFile
    {
        /// <summary>Stores the output path.</summary>
        public readonly string Path;

        /// <summary>Stores normalized UTF-8 text content.</summary>
        public readonly string Content;

        /// <summary>Creates one immutable generated output.</summary>
        /// <param name="path">The output path.</param>
        /// <param name="content">The generated text content.</param>
        public TableGeneratedFile(string path, string content)
        {
            Path    = path;
            Content = content;
        }
    }

    /// <summary>
    /// Stores the observable result of one table compilation.
    /// </summary>
    public readonly struct TableBuildResult
    {
        /// <summary>Indicates whether validation and output commit succeeded.</summary>
        public readonly bool Succeeded;

        /// <summary>Indicates whether any committed output changed.</summary>
        public readonly bool Changed;

        /// <summary>Stores every diagnostic produced by the compilation.</summary>
        public readonly TableDiagnostic[] Diagnostics;

        /// <summary>Creates one immutable build result.</summary>
        /// <param name="succeeded">Whether compilation succeeded.</param>
        /// <param name="changed">Whether any output changed.</param>
        /// <param name="diagnostics">The complete diagnostics.</param>
        public TableBuildResult(bool succeeded, bool changed, TableDiagnostic[] diagnostics)
        {
            Succeeded  = succeeded;
            Changed    = changed;
            Diagnostics = diagnostics;
        }
    }

    /// <summary>
    /// Converts one external source into raw headers and rows.
    /// </summary>
    public interface ITableSourceReader
    {
        /// <summary>Checks whether this reader supports one path.</summary>
        /// <param name="path">The source path.</param>
        /// <returns>True when the source is supported.</returns>
        bool CanRead(string path);

        /// <summary>Reads one source without interpreting field types.</summary>
        /// <param name="path">The source path.</param>
        /// <param name="diagnostics">The diagnostic destination.</param>
        /// <returns>The parsed source, or null when parsing failed.</returns>
        TableSourceData Read(string path, TableDiagnosticCollection diagnostics);
    }

    /// <summary>
    /// Validates and normalizes one registered scalar field type.
    /// </summary>
    public interface ITableFieldType
    {
        /// <summary>Gets the schema type name.</summary>
        string Name { get; }

        /// <summary>Gets the generated C# type name.</summary>
        string CSharpTypeName { get; }

        /// <summary>Converts raw source text into one JSON value.</summary>
        /// <param name="rawValue">The raw source value.</param>
        /// <param name="jsonValue">The normalized JSON value.</param>
        /// <returns>True when conversion succeeded.</returns>
        bool TryConvert(string rawValue, out string jsonValue);
    }

    /// <summary>
    /// Adds project-specific validation after source values have been normalized.
    /// </summary>
    public interface ITableValidator
    {
        /// <summary>Validates one compiled table without mutating it.</summary>
        /// <param name="compilation">The compiled table.</param>
        /// <param name="diagnostics">The diagnostic destination.</param>
        void Validate(TableCompilation compilation, TableDiagnosticCollection diagnostics);
    }

    /// <summary>
    /// Generates strongly typed row source for one validated table.
    /// </summary>
    public interface ITableCodeGenerator
    {
        /// <summary>Generates one source output.</summary>
        /// <param name="schema">The owning schema.</param>
        /// <param name="table">The validated table definition.</param>
        /// <param name="fieldTypes">The resolved field types.</param>
        /// <returns>The generated output.</returns>
        TableGeneratedFile Generate(TableSchema schema, TableDefinition table, ITableFieldType[] fieldTypes);
    }

    /// <summary>
    /// Generates runtime table data for one validated source.
    /// </summary>
    public interface ITableDataWriter
    {
        /// <summary>Gets the default output extension without a leading period.</summary>
        string Extension { get; }

        /// <summary>Generates one data output.</summary>
        /// <param name="schema">The owning schema.</param>
        /// <param name="compilation">The validated table compilation.</param>
        /// <returns>The generated output.</returns>
        TableGeneratedFile Generate(TableSchema schema, TableCompilation compilation);
    }
}
