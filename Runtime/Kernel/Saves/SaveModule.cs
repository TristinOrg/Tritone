using System;
using System.IO;
using Tritone.Kernel;

namespace Tritone.Saves
{
    /// <summary>
    /// Stores serialized save slots through same-directory atomic file replacement.
    /// </summary>
    public sealed class SaveModule : ModuleBase, ISaveService
    {
        // Stores the validated absolute save root.
        private readonly string mRootPath;

        // Converts save objects to and from bytes.
        private readonly ISaveSerializer mSerializer;

        /// <summary>
        /// Initializes local saves beneath one root directory.
        /// </summary>
        /// <param name="rootPath">The absolute or relative save root directory.</param>
        /// <param name="serializer">The save data serializer.</param>
        public SaveModule(string rootPath, ISaveSerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException(
                    "A save root path cannot be null, empty, or whitespace.",
                    nameof(rootPath));

            mRootPath   = Path.GetFullPath(rootPath);
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc />
        protected override void OnConfigure(IServiceRegistry services)
        {
            Directory.CreateDirectory(mRootPath);
            services.AddSingleton<ISaveService>(this);
        }

        /// <inheritdoc />
        public bool Exists(string slot)
        {
            var path = ResolvePath(slot);
            return File.Exists(path) || File.Exists(GetBackupPath(path));
        }

        /// <inheritdoc />
        public void Save<T>(string slot, T value) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var path       = ResolvePath(slot);
            var temporary  = GetTemporaryPath(path);
            var backup     = GetBackupPath(path);
            var serialized = mSerializer.Serialize(value) ??
                             throw new InvalidOperationException(
                                 "The save serializer returned null bytes.");
            Directory.CreateDirectory(mRootPath);
            File.WriteAllBytes(temporary, serialized);
            var movedOriginal = false;
            var installedNew  = false;
            try
            {
                if (File.Exists(backup))
                    File.Delete(backup);
                if (File.Exists(path))
                {
                    File.Move(path, backup);
                    movedOriginal = true;
                }
                File.Move(temporary, path);
                installedNew = true;
                if (File.Exists(backup))
                    File.Delete(backup);
            }
            catch
            {
                if (installedNew && File.Exists(path))
                    File.Delete(path);
                if (movedOriginal && File.Exists(backup))
                    File.Move(backup, path);
                if (File.Exists(temporary))
                    File.Delete(temporary);
                throw;
            }
        }

        /// <inheritdoc />
        public T Load<T>(string slot) where T : class
        {
            var path = ResolvePath(slot);
            Recover(path);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Save slot '{slot}' was not found.", path);

            return mSerializer.Deserialize<T>(File.ReadAllBytes(path)) ??
                   throw new InvalidDataException($"Save slot '{slot}' deserialized to null.");
        }

        /// <inheritdoc />
        public bool TryLoad<T>(string slot, out T value) where T : class
        {
            if (!Exists(slot))
            {
                value = null;
                return false;
            }

            value = Load<T>(slot);
            return true;
        }

        /// <inheritdoc />
        public bool Delete(string slot)
        {
            var path    = ResolvePath(slot);
            var backup  = GetBackupPath(path);
            var deleted = false;
            if (File.Exists(path))
            {
                File.Delete(path);
                deleted = true;
            }
            if (File.Exists(backup))
            {
                File.Delete(backup);
                deleted = true;
            }
            var temporary = GetTemporaryPath(path);
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
                deleted = true;
            }
            return deleted;
        }

        /// <summary>
        /// Restores a backup left by an interrupted atomic replacement.
        /// </summary>
        /// <param name="path">The resolved primary slot path.</param>
        private static void Recover(string path)
        {
            var temporary = GetTemporaryPath(path);
            if (File.Exists(temporary))
                File.Delete(temporary);

            var backup = GetBackupPath(path);
            if (!File.Exists(path) && File.Exists(backup))
                File.Move(backup, path);
            else if (File.Exists(path) && File.Exists(backup))
                File.Delete(backup);
        }

        /// <summary>
        /// Resolves and validates one portable slot name beneath the save root.
        /// </summary>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <returns>The absolute slot file path.</returns>
        private string ResolvePath(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot))
                throw new ArgumentException(
                    "A save slot cannot be null, empty, or whitespace.",
                    nameof(slot));
            if (slot.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                slot.Contains("/") ||
                slot.Contains("\\"))
                throw new ArgumentException(
                    "A save slot must be a portable file name without directories.",
                    nameof(slot));

            return Path.Combine(mRootPath, slot + ".save");
        }

        /// <summary>
        /// Gets the same-directory temporary path for one slot.
        /// </summary>
        /// <param name="path">The primary slot path.</param>
        /// <returns>The temporary file path.</returns>
        private static string GetTemporaryPath(string path)
        {
            return path + ".tmp";
        }

        /// <summary>
        /// Gets the same-directory backup path for one slot.
        /// </summary>
        /// <param name="path">The primary slot path.</param>
        /// <returns>The backup file path.</returns>
        private static string GetBackupPath(string path)
        {
            return path + ".bak";
        }
    }
}
