using System;
using System.IO;

namespace Tritone.Content
{
    /// <summary>
    /// Defines validated local storage used by one transactional content updater.
    /// </summary>
    public sealed class ContentUpdateSettings
    {
        // Defines the private transaction directory beneath the content root.
        private const string TransactionDirectoryName = ".tritone-update";

        // Stores the normalized root including its final directory separator.
        private readonly string mRootPrefix;

        // Gets the normalized absolute content root.
        public string RootPath { get; }

        // Gets the relative local manifest file name.
        public string ManifestFileName { get; }

        // Gets the absolute active local manifest path.
        public string ManifestPath { get; }

        // Gets the private absolute transaction directory path.
        public string TransactionPath { get; }

        /// <summary>
        /// Initializes validated local content storage settings.
        /// </summary>
        /// <param name="rootPath">The absolute directory containing active bundles and the local manifest.</param>
        /// <param name="manifestFileName">The relative local manifest file name.</param>
        public ContentUpdateSettings(string rootPath,
                                     string manifestFileName = "content-manifest.json")
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("A content root path is required.", nameof(rootPath));

            RootPath        = Path.GetFullPath(rootPath);
            mRootPrefix     = EnsureDirectorySeparator(RootPath);
            ManifestFileName = ValidateRelativePath(manifestFileName, nameof(manifestFileName));
            ManifestPath     = ResolveRelativePath(ManifestFileName);
            TransactionPath  = ResolveRelativePath(TransactionDirectoryName);
        }

        /// <summary>
        /// Resolves and validates one bundle file beneath the configured content root.
        /// </summary>
        /// <param name="fileName">The manifest-provided relative bundle file name.</param>
        /// <returns>The normalized absolute bundle file path.</returns>
        public string ResolveBundlePath(string fileName)
        {
            var relativePath = ValidateRelativePath(fileName, nameof(fileName));
            if (string.Equals(relativePath, ManifestFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Bundle file '{fileName}' conflicts with the local manifest.");
            if (relativePath.StartsWith(TransactionDirectoryName + "/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(relativePath, TransactionDirectoryName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Bundle file '{fileName}' conflicts with private update storage.");

            return ResolveRelativePath(relativePath);
        }

        /// <summary>
        /// Resolves one already validated relative path beneath the content root.
        /// </summary>
        /// <param name="relativePath">The validated forward-slash relative path.</param>
        /// <returns>The normalized absolute path beneath the root.</returns>
        internal string ResolveRelativePath(string relativePath)
        {
            var platformPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            var fullPath     = Path.GetFullPath(Path.Combine(RootPath, platformPath));
            var comparison   = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!fullPath.StartsWith(mRootPrefix, comparison))
                throw new InvalidOperationException($"Content path '{relativePath}' escapes the configured root.");
            return fullPath;
        }

        /// <summary>
        /// Validates a portable forward-slash relative content path.
        /// </summary>
        /// <param name="relativePath">The relative path supplied by configuration or a manifest.</param>
        /// <param name="parameterName">The public parameter name used by validation errors.</param>
        /// <returns>The validated unchanged relative path.</returns>
        internal static string ValidateRelativePath(string relativePath, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("A relative content path is required.", parameterName);
            if (Path.IsPathRooted(relativePath) || relativePath.IndexOf('\\') >= 0)
                throw new ArgumentException("Content paths must be portable forward-slash relative paths.", parameterName);

            var segments = relativePath.Split('/');
            for (int i = 0, cnt = segments.Length; i < cnt; i++)
            {
                var segment = segments[i];
                if (segment.Length == 0 || segment == "." || segment == "..")
                    throw new ArgumentException("Content paths cannot contain empty, current, or parent segments.", parameterName);
            }
            return relativePath;
        }

        /// <summary>
        /// Appends a directory separator when one is not already present.
        /// </summary>
        /// <param name="path">The normalized absolute directory path.</param>
        /// <returns>The path ending in a directory separator.</returns>
        private static string EnsureDirectorySeparator(string path)
        {
            if (path.Length > 0 &&
                (path[path.Length - 1] == Path.DirectorySeparatorChar ||
                 path[path.Length - 1] == Path.AltDirectorySeparatorChar))
                return path;

            return path + Path.DirectorySeparatorChar;
        }
    }
}
