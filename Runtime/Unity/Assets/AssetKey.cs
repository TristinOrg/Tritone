using System;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Identifies one cached request by provider path and requested type.
    /// </summary>
    internal readonly struct AssetKey : IEquatable<AssetKey>
    {
        // Stores the provider-specific asset path.
        internal readonly string Path;

        // Stores the requested runtime asset type.
        internal readonly Type AssetType;

        // Stores the precomputed dictionary hash.
        private readonly int mHashCode;

        /// <summary>
        /// Initializes one immutable asset cache key.
        /// </summary>
        internal AssetKey(string path, Type assetType)
        {
            Path      = path;
            AssetType = assetType;
            mHashCode = unchecked((StringComparer.Ordinal.GetHashCode(path) * 397) ^ assetType.GetHashCode());
        }

        /// <summary>
        /// Determines whether another key represents the same asset request.
        /// </summary>
        public bool Equals(AssetKey other)
        {
            return AssetType == other.AssetType &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether an object is an equal asset key.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is AssetKey other && Equals(other);
        }

        /// <summary>
        /// Returns the precomputed dictionary hash.
        /// </summary>
        public override int GetHashCode()
        {
            return mHashCode;
        }
    }
}
