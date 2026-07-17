using System;
using System.IO;
using UnityEngine;

namespace Tritone.Editor.CodeGeneration
{
    /// <summary>
    /// Provides shared schema loading, naming, and source validation.
    /// </summary>
    internal static class GenerationUtility
    {
        /// <summary>
        /// Reads and deserializes one required JSON schema.
        /// </summary>
        /// <typeparam name="TSchema">The serializable schema root type.</typeparam>
        /// <param name="path">The project-relative schema path.</param>
        /// <returns>The parsed schema instance.</returns>
        internal static TSchema ReadSchema<TSchema>(string path) where TSchema : class
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Tritone generation schema was not found.", path);
            var schema = JsonUtility.FromJson<TSchema>(File.ReadAllText(path));
            return schema ?? throw new InvalidDataException($"Schema '{path}' is empty or invalid.");
        }

        /// <summary>
        /// Validates one C# identifier without invoking compiler services.
        /// </summary>
        /// <param name="value">The identifier to validate.</param>
        /// <param name="label">The schema label used in validation errors.</param>
        internal static void ValidateIdentifier(string value, string label)
        {
            if (string.IsNullOrEmpty(value) ||
                (!char.IsLetter(value[0]) && value[0] != '_'))
                throw new InvalidDataException($"{label} '{value}' is not a valid C# identifier.");
            for (int i = 1, cnt = value.Length; i < cnt; i++)
            {
                if (!char.IsLetterOrDigit(value[i]) && value[i] != '_')
                    throw new InvalidDataException($"{label} '{value}' is not a valid C# identifier.");
            }
        }
    }
}
