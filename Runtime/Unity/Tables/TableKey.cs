using System;

namespace Tritone.Unity.Tables
{
    /// <summary>
    /// Identifies one cached table by path, key type, and row type.
    /// </summary>
    internal readonly struct TableKey : IEquatable<TableKey>
    {
        // Stores the asset-provider path.
        internal readonly string Path;

        // Stores the table key type.
        internal readonly Type KeyType;

        // Stores the table row type.
        internal readonly Type RowType;

        // Stores the precomputed dictionary hash.
        private readonly int mHashCode;

        /// <summary>
        /// Initializes one immutable table cache key.
        /// </summary>
        /// <param name="path">The asset-provider path.</param>
        /// <param name="keyType">The row key type.</param>
        /// <param name="rowType">The configuration row type.</param>
        internal TableKey(string path, Type keyType, Type rowType)
        {
            Path      = path;
            KeyType   = keyType;
            RowType   = rowType;
            mHashCode = unchecked((StringComparer.Ordinal.GetHashCode(path) * 397) ^
                                  (keyType.GetHashCode() * 31) ^
                                  rowType.GetHashCode());
        }

        /// <summary>
        /// Determines whether another key represents the same table request.
        /// </summary>
        /// <param name="other">The other table key.</param>
        /// <returns>True when every key component matches; otherwise, false.</returns>
        public bool Equals(TableKey other)
        {
            return KeyType == other.KeyType &&
                   RowType == other.RowType &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether an object is an equal table key.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True when the object is an equal table key; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is TableKey other && Equals(other);
        }

        /// <summary>
        /// Returns the precomputed dictionary hash.
        /// </summary>
        /// <returns>The precomputed hash code.</returns>
        public override int GetHashCode()
        {
            return mHashCode;
        }
    }
}
