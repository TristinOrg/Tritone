#if UNITY_EDITOR
using System;
using System.IO;
using Tritone.Editor.CodeGeneration;
using Tritone.Editor.Tables;
using UnityEditor;
using UnityEngine;

namespace AfterHours.Editor
{
    /// <summary>Runs Tritone's transactional table compiler on checked-in case data.</summary>
    public static class AfterHoursTableAuthoring
    {
        private const string SchemaPath = "Assets/AfterHours/Schemas/Tables.json";

        [MenuItem("Tritone/After Hours/Regenerate Table Outputs")]
        public static void RegenerateTables()
        {
            if (!File.Exists(SchemaPath))
            {
                Debug.LogError("Import the After Hours RPG sample before compiling its tables.");
                return;
            }

            var schema = JsonUtility.FromJson<TableSchema>(File.ReadAllText(SchemaPath));
            var compiler = new TableCompilerBuilder()
                .AddSourceReader(new DelimitedTableSourceReader())
                .AddDefaultFieldTypes()
                .AddValidator(new AfterHoursClueTableValidator())
                .UseCodeGenerator(new CSharpTableCodeGenerator())
                .UseDataWriter(new JsonTableDataWriter())
                .Build();
            var result = compiler.Compile(schema);
            foreach (var diagnostic in result.Diagnostics)
                Debug.Log($"[{diagnostic.Code}] {diagnostic.Message}");
            if (!result.Succeeded)
                throw new InvalidOperationException("After Hours table generation failed. See the preceding diagnostics.");
            if (result.Changed)
                AssetDatabase.Refresh();
            Debug.Log(result.Changed ? "After Hours table code and runtime data regenerated." : "After Hours table outputs are current.");
        }
    }

    /// <summary>Adds one game-specific guard without modifying Tritone's shared table compiler.</summary>
    internal sealed class AfterHoursClueTableValidator : ITableValidator
    {
        public void Validate(TableCompilation compilation, TableDiagnosticCollection diagnostics)
        {
            if (compilation?.Definition?.Name != "Clue" || compilation.Values.Length >= 2)
                return;

            diagnostics.Error("AFTER-HOURS-CLUE-001",
                "The Last Stop case requires at least two authored evidence clues.",
                new TableSourceLocation(compilation.Definition.Source, 0, 0));
        }
    }
}
#endif
