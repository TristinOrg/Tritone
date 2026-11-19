#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AfterHours.Editor
{
    /// <summary>Runs Tritone's generated message codec pipeline for the case relay schema.</summary>
    public static class AfterHoursNetworkAuthoring
    {
        private const string SchemaPath = "Assets/AfterHours/Schemas/Network.json";

        [MenuItem("Tritone/After Hours/Regenerate Network Messages")]
        public static void RegenerateNetworkMessages()
        {
            if (!File.Exists(SchemaPath))
            {
                Debug.LogError("Import the After Hours RPG sample before generating its protocol messages.");
                return;
            }

            // Tritone keeps its generator implementation internal; the sample invokes the
            // same typed schema entry point used by Tritone's built-in menu.
            Assembly generatorAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "Tritone.Editor");
            Type schemaType = generatorAssembly?.GetType("Tritone.Editor.CodeGeneration.NetworkSchema");
            Type generatorType = generatorAssembly?.GetType("Tritone.Editor.CodeGeneration.NetworkCodeGenerator");
            if (schemaType == null || generatorType == null)
                throw new InvalidOperationException("The installed Tritone.Editor assembly does not expose its network schema generator.");
            MethodInfo generate = generatorType?.GetMethod("Generate", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { schemaType }, null);
            if (generate == null)
                throw new InvalidOperationException("The installed Tritone.Editor assembly does not expose its network schema generator.");

            object schema = JsonUtility.FromJson(File.ReadAllText(SchemaPath), schemaType);
            try
            {
                generate.Invoke(null, new[] { schema });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
            AssetDatabase.Refresh();
            Debug.Log("After Hours network messages and protocol fingerprint regenerated from Schemas/Network.json.");
        }
    }
}
#endif
