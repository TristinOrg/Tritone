using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tritone.Content
{
    /// <summary>
    /// Describes one immutable versioned content bundle and its direct dependencies.
    /// </summary>
    public sealed class ContentBundle
    {
        // Stores direct bundle dependencies in deterministic declaration order.
        private readonly string[] mDependencies;

        // Exposes dependencies without allowing the private array to be cast and modified.
        private readonly ReadOnlyCollection<string> mReadOnlyDependencies;

        // Gets the stable logical bundle name.
        public string Name { get; }

        // Gets the platform-specific file name stored locally or remotely.
        public string FileName { get; }

        // Gets the content hash used to detect and verify file changes.
        public string Hash { get; }

        // Gets the expected bundle file size in bytes.
        public long Size { get; }

        // Gets the direct logical bundle dependencies.
        public IReadOnlyList<string> Dependencies => mReadOnlyDependencies;

        /// <summary>
        /// Initializes one immutable content bundle definition.
        /// </summary>
        /// <param name="name">The stable logical bundle name.</param>
        /// <param name="fileName">The platform-specific bundle file name.</param>
        /// <param name="hash">The content hash written by the build pipeline.</param>
        /// <param name="size">The expected bundle file size in bytes.</param>
        /// <param name="dependencies">The direct logical bundle dependencies.</param>
        public ContentBundle(string name,
                             string fileName,
                             string hash,
                             long size,
                             params string[] dependencies)
        {
            ContentValidation.ValidateValue(name, nameof(name));
            ContentValidation.ValidateValue(fileName, nameof(fileName));
            ContentValidation.ValidateValue(hash, nameof(hash));
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), size, "Content bundle size cannot be negative.");

            Name          = name;
            FileName      = fileName;
            Hash          = hash;
            Size          = size;
            mDependencies = dependencies == null || dependencies.Length == 0
                ? Array.Empty<string>()
                : (string[])dependencies.Clone();
            mReadOnlyDependencies = Array.AsReadOnly(mDependencies);

            HashSet<string> uniqueDependencies = new(StringComparer.Ordinal);
            for (int i = 0, cnt = mDependencies.Length; i < cnt; i++)
            {
                var dependency = mDependencies[i];
                ContentValidation.ValidateValue(dependency, nameof(dependencies));
                if (string.Equals(Name, dependency, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Content bundle '{Name}' cannot depend on itself.");
                if (!uniqueDependencies.Add(dependency))
                    throw new InvalidOperationException($"Content bundle '{Name}' contains duplicate dependency '{dependency}'.");
            }
        }
    }
}
