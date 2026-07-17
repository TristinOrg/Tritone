using UnityEditor;

namespace Tritone.Editor.CodeGeneration
{
    /// <summary>
    /// Coordinates all project code generators through one Unity menu action.
    /// </summary>
    internal static class TritoneCodeGenerator
    {
        private const string TableSchemaPath = "Assets/Tritone/Tables.json";
        private const string NetworkSchemaPath = "Assets/Tritone/Network.json";

        /// <summary>
        /// Generates every configured Tritone source set and refreshes Unity once.
        /// </summary>
        [MenuItem("Tritone/Generate/All")]
        internal static void GenerateAll()
        {
            var tables = GenerationUtility.ReadSchema<TableSchema>(TableSchemaPath);
            var network = GenerationUtility.ReadSchema<NetworkSchema>(NetworkSchemaPath);
            var changed = TableCodeGenerator.Generate(tables);
            changed |= NetworkCodeGenerator.Generate(network);
            if (changed)
                AssetDatabase.Refresh();
        }
    }
}
