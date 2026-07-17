using System;
using System.IO;
using System.Text;

namespace Tritone.Editor.CodeGeneration
{
    /// <summary>
    /// Writes generated files only when their normalized content changes.
    /// </summary>
    internal static class GeneratedFileWriter
    {
        // Uses deterministic UTF-8 output without a byte-order mark.
        private static readonly Encoding sEncoding = new UTF8Encoding(false);

        /// <summary>
        /// Writes one generated source file atomically when it has changed.
        /// </summary>
        /// <param name="path">The absolute or project-relative output path.</param>
        /// <param name="content">The complete generated source content.</param>
        /// <returns>True when the target file changed; otherwise, false.</returns>
        internal static bool WriteIfChanged(string path, string content)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A generated output path is required.", nameof(path));
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            var normalized = Normalize(content);
            if (File.Exists(path) &&
                string.Equals(Normalize(File.ReadAllText(path)), normalized, StringComparison.Ordinal))
                return false;

            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            Directory.CreateDirectory(directory);
            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, normalized, sEncoding);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temporaryPath, path);
            return true;
        }

        /// <summary>
        /// Normalizes generated text to stable LF line endings.
        /// </summary>
        /// <param name="content">The source text to normalize.</param>
        /// <returns>The normalized source text.</returns>
        private static string Normalize(string content)
        {
            return content.Replace("\r\n", "\n").Replace('\r', '\n');
        }
    }
}
