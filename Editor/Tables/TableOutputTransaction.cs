using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tritone.Editor.Tables
{
    /// <summary>
    /// Commits changed generated files atomically and restores prior files after an I/O failure.
    /// </summary>
    internal static class TableOutputTransaction
    {
        /// <summary>Writes all changed outputs as one rollback-capable transaction.</summary>
        /// <param name="files">The validated in-memory outputs.</param>
        /// <returns>True when at least one output changed.</returns>
        internal static bool Commit(List<TableGeneratedFile> files)
        {
            var changed = new List<TableGeneratedFile>();
            foreach (var file in files)
            {
                var content = Normalize(file.Content);
                if (File.Exists(file.Path) && string.Equals(Normalize(File.ReadAllText(file.Path)), content, StringComparison.Ordinal))
                {
                    continue;
                }
                changed.Add(new TableGeneratedFile(file.Path, content));
            }
            if (changed.Count == 0)
            {
                return false;
            }

            var committed = 0;
            var hadOriginal = new bool[changed.Count];
            try
            {
                foreach (var file in changed)
                {
                    var fullPath  = Path.GetFullPath(file.Path);
                    var directory = Path.GetDirectoryName(fullPath);
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(GetTemporaryPath(fullPath), file.Content, new UTF8Encoding(false));
                }
                for (var i = 0; i < changed.Count; i++)
                {
                    var file       = changed[i];
                    var fullPath   = Path.GetFullPath(file.Path);
                    var backupPath = GetBackupPath(fullPath);
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                    hadOriginal[i] = File.Exists(fullPath);
                    if (hadOriginal[i])
                    {
                        File.Move(fullPath, backupPath);
                    }
                    File.Move(GetTemporaryPath(fullPath), fullPath);
                    committed++;
                }
                foreach (var file in changed)
                {
                    var backupPath = GetBackupPath(Path.GetFullPath(file.Path));
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                }
                return true;
            }
            catch
            {
                for (var i = changed.Count - 1; i >= 0; i--)
                {
                    var fullPath   = Path.GetFullPath(changed[i].Path);
                    var backupPath = GetBackupPath(fullPath);
                    if (File.Exists(backupPath))
                    {
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                        }
                        File.Move(backupPath, fullPath);
                    }
                    else if (i < committed && !hadOriginal[i] && File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
                throw;
            }
            finally
            {
                foreach (var file in changed)
                {
                    var temporaryPath = GetTemporaryPath(Path.GetFullPath(file.Path));
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
            }
        }

        /// <summary>Normalizes generated text to LF line endings.</summary>
        /// <param name="content">The generated content.</param>
        /// <returns>The normalized content.</returns>
        private static string Normalize(string content)
        {
            return content.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        /// <summary>Gets the transaction temporary path.</summary>
        /// <param name="path">The final path.</param>
        /// <returns>The temporary path.</returns>
        private static string GetTemporaryPath(string path)
        {
            return path + ".tritone.tmp";
        }

        /// <summary>Gets the transaction backup path.</summary>
        /// <param name="path">The final path.</param>
        /// <returns>The backup path.</returns>
        private static string GetBackupPath(string path)
        {
            return path + ".tritone.bak";
        }
    }
}
