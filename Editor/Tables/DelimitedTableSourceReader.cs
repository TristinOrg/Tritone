using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tritone.Editor.Tables
{
    /// <summary>
    /// Reads RFC-style quoted CSV and TSV sources into raw table rows.
    /// </summary>
    public sealed class DelimitedTableSourceReader : ITableSourceReader
    {
        /// <inheritdoc />
        public bool CanRead(string path)
        {
            var extension = Path.GetExtension(path);
            return extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) || extension.Equals(".tsv", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public TableSourceData Read(string path, TableDiagnosticCollection diagnostics)
        {
            if (!File.Exists(path))
            {
                diagnostics.Error("TRT-TABLE-1001", $"Table source '{path}' does not exist.", new TableSourceLocation(path, 0, 0));
                return null;
            }
            var delimiter = Path.GetExtension(path).Equals(".tsv", StringComparison.OrdinalIgnoreCase) ? '\t' : ',';
            var rows      = Parse(File.ReadAllText(path), delimiter, path, diagnostics);
            if (rows == null || rows.Count == 0)
            {
                diagnostics.Error("TRT-TABLE-1002", "Table source has no header row.", new TableSourceLocation(path, 1, 1));
                return null;
            }
            var headers = rows[0].Cells;
            rows.RemoveAt(0);
            return new TableSourceData(path, headers, rows.ToArray());
        }

        /// <summary>Parses quoted delimited text while preserving source row numbers.</summary>
        /// <param name="text">The complete source text.</param>
        /// <param name="delimiter">The field delimiter.</param>
        /// <param name="source">The source path used by diagnostics.</param>
        /// <param name="diagnostics">The diagnostic destination.</param>
        /// <returns>The parsed rows, or null when quotes are malformed.</returns>
        private static List<TableSourceRow> Parse(string text, char delimiter, string source, TableDiagnosticCollection diagnostics)
        {
            var rows      = new List<TableSourceRow>();
            var cells     = new List<string>();
            var cell      = new StringBuilder();
            var quoted    = false;
            var rowNumber = 1;
            for (var i = 0; i < text.Length; i++)
            {
                var character = text[i];
                if (quoted)
                {
                    if (character == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        cell.Append(character);
                    }
                    continue;
                }
                if (character == '"' && cell.Length == 0)
                {
                    quoted = true;
                    continue;
                }
                if (character == delimiter)
                {
                    cells.Add(cell.ToString());
                    cell.Clear();
                    continue;
                }
                if (character is '\r' or '\n')
                {
                    if (character == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }
                    cells.Add(cell.ToString());
                    cell.Clear();
                    if (!IsEmpty(cells))
                    {
                        rows.Add(new TableSourceRow(rowNumber, cells.ToArray()));
                    }
                    cells.Clear();
                    rowNumber++;
                    continue;
                }
                cell.Append(character);
            }
            if (quoted)
            {
                diagnostics.Error("TRT-TABLE-1003", "Table source contains an unclosed quoted field.", new TableSourceLocation(source, rowNumber, cells.Count + 1));
                return null;
            }
            if (cell.Length > 0 || cells.Count > 0)
            {
                cells.Add(cell.ToString());
                if (!IsEmpty(cells))
                {
                    rows.Add(new TableSourceRow(rowNumber, cells.ToArray()));
                }
            }
            return rows;
        }

        /// <summary>Checks whether one source row contains no values.</summary>
        /// <param name="cells">The source cells.</param>
        /// <returns>True when every cell is empty.</returns>
        private static bool IsEmpty(List<string> cells)
        {
            foreach (var value in cells)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
