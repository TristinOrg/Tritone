using System.Collections.Generic;

namespace Tritone.Editor.Tables
{
    /// <summary>
    /// Collects ordered table diagnostics and tracks whether compilation must stop.
    /// </summary>
    public sealed class TableDiagnosticCollection
    {
        /// <summary>Stores diagnostics in deterministic production order.</summary>
        private readonly List<TableDiagnostic> mItems = new();

        /// <summary>Gets whether at least one error has been recorded.</summary>
        public bool HasErrors { get; private set; }

        /// <summary>Adds one diagnostic.</summary>
        /// <param name="diagnostic">The diagnostic to append.</param>
        public void Add(TableDiagnostic diagnostic)
        {
            mItems.Add(diagnostic);
            if (diagnostic.Severity == ETableDiagnosticSeverity.Error)
            {
                HasErrors = true;
            }
        }

        /// <summary>Adds one error diagnostic.</summary>
        /// <param name="code">The stable diagnostic code.</param>
        /// <param name="message">The diagnostic message.</param>
        /// <param name="location">The source position.</param>
        public void Error(string code, string message, TableSourceLocation location)
        {
            Add(new TableDiagnostic(ETableDiagnosticSeverity.Error, code, message, location));
        }

        /// <summary>Copies all diagnostics into an immutable result array.</summary>
        /// <returns>The ordered diagnostic array.</returns>
        public TableDiagnostic[] ToArray()
        {
            return mItems.ToArray();
        }
    }
}
