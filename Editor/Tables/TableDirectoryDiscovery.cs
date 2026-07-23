using System;
using System.Collections.Generic;
using System.IO;
using Tritone.Editor.CodeGeneration;

namespace Tritone.Editor.Tables
{
    /// <summary>
    /// Discovers self-describing table sources from configured directories.
    /// </summary>
    internal static class TableDirectoryDiscovery
    {
        /// <summary>Combines explicit definitions with deterministically discovered CSV and TSV files.</summary>
        /// <param name="schema">The table schema.</param>
        /// <param name="diagnostics">The diagnostic destination.</param>
        /// <returns>All table definitions in deterministic order.</returns>
        internal static TableDefinition[] Discover(TableSchema schema, TableDiagnosticCollection diagnostics)
        {
            var tables = new List<TableDefinition>();
            if (schema?.Tables != null)
            {
                tables.AddRange(schema.Tables);
            }
            if (schema?.SourceDirectories == null)
            {
                return tables.ToArray();
            }
            foreach (var directory in schema.SourceDirectories)
            {
                DiscoverDirectory(directory, tables, diagnostics);
            }
            return tables.ToArray();
        }

        /// <summary>Discovers supported sources below one configured directory.</summary>
        /// <param name="directory">The configured source directory.</param>
        /// <param name="tables">The discovered table destination.</param>
        /// <param name="diagnostics">The diagnostic destination.</param>
        private static void DiscoverDirectory(string directory, List<TableDefinition> tables, TableDiagnosticCollection diagnostics)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                diagnostics.Error("TRT-TABLE-1101", $"Table source directory '{directory}' does not exist.", new TableSourceLocation(directory, 0, 0));
                return;
            }
            var root  = Path.GetFullPath(directory);
            var files = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            var runtimeRoot = new DirectoryInfo(root).Name;
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file);
                if (!extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".tsv", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var relative       = Path.GetRelativePath(root, file);
                var relativeNoType = Path.ChangeExtension(relative, null);
                var runtimePath    = (runtimeRoot + "/" + relativeNoType).Replace('\\', '/');
                tables.Add(new TableDefinition
                {
                    Name     = Path.GetFileNameWithoutExtension(file),
                    Path     = runtimePath,
                    Source   = file,
                    DataFile = relativeNoType + ".json"
                });
            }
        }
    }
}
