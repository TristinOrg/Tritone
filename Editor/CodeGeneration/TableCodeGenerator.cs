using System;
using System.Collections.Generic;
using System.IO;
using Tritone.Editor.Tables;
using UnityEditor;
using UnityEngine;

namespace Tritone.Editor.CodeGeneration
{
    /// <summary>
    /// Exposes the default Table Compiler through the established Tritone generation entry point.
    /// </summary>
    internal static class TableCodeGenerator
    {
        /// <summary>Stores the conventional project schema path.</summary>
        private const string SchemaPath = "Assets/Tritone/Tables.json";

        /// <summary>Stores the default generated source directory.</summary>
        private const string DefaultOutputPath = "Assets/Generated/Tritone/Tables";

        /// <summary>Compiles configured tables and refreshes Unity when outputs changed.</summary>
        [MenuItem("Tritone/Generate/Tables")]
        internal static void Generate()
        {
            var schema = GenerationUtility.ReadSchema<TableSchema>(SchemaPath);
            var result = Compile(schema);
            LogDiagnostics(result.Diagnostics);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Tritone table compilation failed. See structured diagnostics above.");
            }
            if (result.Changed)
            {
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Compiles one schema through the default extensible compiler.</summary>
        /// <param name="schema">The schema to compile.</param>
        /// <returns>True when generated outputs changed.</returns>
        internal static bool Generate(TableSchema schema)
        {
            var result = Compile(schema);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(GetFirstError(result.Diagnostics));
            }
            return result.Changed;
        }

        /// <summary>Compiles one schema and returns its structured result.</summary>
        /// <param name="schema">The schema to compile.</param>
        /// <returns>The structured result.</returns>
        internal static TableBuildResult Compile(TableSchema schema)
        {
            var result = TableCompiler.CreateDefault().Compile(schema);
            if (!result.Succeeded)
            {
                return result;
            }
            var removed = RemoveStaleFiles(schema);
            return new TableBuildResult(true, result.Changed || removed, result.Diagnostics);
        }

        /// <summary>Removes generated table sources no longer represented by the schema.</summary>
        /// <param name="schema">The compiled schema.</param>
        /// <returns>True when a stale source or metadata file was removed.</returns>
        private static bool RemoveStaleFiles(TableSchema schema)
        {
            var outputPath = string.IsNullOrWhiteSpace(schema.OutputPath) ? DefaultOutputPath : schema.OutputPath;
            if (!Directory.Exists(outputPath))
            {
                return false;
            }
            var expectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var diagnostics   = new TableDiagnosticCollection();
            var tables        = TableDirectoryDiscovery.Discover(schema, diagnostics);
            foreach (var table in tables)
            {
                expectedFiles.Add(Path.GetFullPath(Path.Combine(outputPath, table.Name + "Table.Generated.cs")));
            }
            var changed = false;
            foreach (var file in Directory.GetFiles(outputPath, "*Table.Generated.cs"))
            {
                if (expectedFiles.Contains(Path.GetFullPath(file)))
                {
                    continue;
                }
                File.Delete(file);
                var metaPath = file + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
                changed = true;
            }
            return changed;
        }

        /// <summary>Logs diagnostics with source coordinates through the Unity Console.</summary>
        /// <param name="diagnostics">The diagnostics to log.</param>
        private static void LogDiagnostics(TableDiagnostic[] diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                var location = diagnostic.Location;
                var prefix   = string.IsNullOrWhiteSpace(location.Source) ? diagnostic.Code : $"{diagnostic.Code} {location.Source}:{location.Row}:{location.Column}";
                var message  = $"[{prefix}] {diagnostic.Message}";
                switch (diagnostic.Severity)
                {
                    case ETableDiagnosticSeverity.Info: Debug.Log(message); break;
                    case ETableDiagnosticSeverity.Warning: Debug.LogWarning(message); break;
                    case ETableDiagnosticSeverity.Error: Debug.LogError(message); break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>Gets the first blocking error for programmatic compatibility calls.</summary>
        /// <param name="diagnostics">The build diagnostics.</param>
        /// <returns>The first error message.</returns>
        private static string GetFirstError(TableDiagnostic[] diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Severity == ETableDiagnosticSeverity.Error)
                {
                    return $"[{diagnostic.Code}] {diagnostic.Message}";
                }
            }
            return "Tritone table compilation failed.";
        }
    }
}
