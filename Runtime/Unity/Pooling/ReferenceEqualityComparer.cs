using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Tritone.Unity.Pooling
{
    /// <summary>
    /// Compares managed objects by identity instead of overridable value equality.
    /// </summary>
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        // Stores the shared stateless comparer.
        internal static readonly ReferenceEqualityComparer Instance = new();

        /// <summary>
        /// Determines whether two references point to the same object.
        /// </summary>
        public new bool Equals(object left, object right)
        {
            return ReferenceEquals(left, right);
        }

        /// <summary>
        /// Gets an identity-based hash code that ignores object overrides.
        /// </summary>
        public int GetHashCode(object instance)
        {
            return RuntimeHelpers.GetHashCode(instance);
        }
    }
}
