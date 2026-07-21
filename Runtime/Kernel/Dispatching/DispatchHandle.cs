using System;

namespace Tritone.Dispatching
{
    /// <summary>
    /// Identifies one pending main-thread callback without retaining its delegate.
    /// </summary>
    public readonly struct DispatchHandle : IEquatable<DispatchHandle>
    {
        /// <summary>
        /// Initializes one handle with a non-zero scheduler identifier.
        /// </summary>
        /// <param name="id">The unique scheduler identifier.</param>
        internal DispatchHandle(long id)
        {
            Id = id;
        }

        /// <summary>
        /// Gets an invalid dispatch handle.
        /// </summary>
        public static DispatchHandle Invalid => default;

        /// <summary>
        /// Gets whether this handle contains a scheduler identifier.
        /// </summary>
        public bool IsValid => Id > 0;

        /// <summary>
        /// Gets the internal scheduler identifier.
        /// </summary>
        internal long Id { get; }

        /// <inheritdoc />
        public bool Equals(DispatchHandle other)
        {
            return Id == other.Id;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is DispatchHandle other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Determines whether two handles identify the same callback.
        /// </summary>
        /// <param name="left">The left dispatch handle.</param>
        /// <param name="right">The right dispatch handle.</param>
        /// <returns>True when both handles contain the same identifier.</returns>
        public static bool operator ==(DispatchHandle left, DispatchHandle right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two handles identify different callbacks.
        /// </summary>
        /// <param name="left">The left dispatch handle.</param>
        /// <param name="right">The right dispatch handle.</param>
        /// <returns>True when the handles contain different identifiers.</returns>
        public static bool operator !=(DispatchHandle left, DispatchHandle right)
        {
            return !left.Equals(right);
        }
    }
}
