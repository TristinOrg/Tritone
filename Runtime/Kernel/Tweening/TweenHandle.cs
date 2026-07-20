using System;

namespace Tritone.Tweening
{
    /// <summary>
    /// Identifies one scheduled tween or sequence.
    /// </summary>
    public readonly struct TweenHandle : IEquatable<TweenHandle>
    {
        /// <summary>
        /// Initializes one non-zero tween identifier.
        /// </summary>
        /// <param name="id">The unique scheduler identifier.</param>
        internal TweenHandle(ulong id)
        {
            Id = id;
        }

        /// <summary>Gets an invalid tween handle.</summary>
        public static TweenHandle Invalid => default;

        /// <summary>Gets whether this handle contains a valid identifier.</summary>
        public bool IsValid => Id != 0;

        /// <summary>Gets the internal scheduler identifier.</summary>
        internal ulong Id { get; }

        /// <inheritdoc />
        public bool Equals(TweenHandle other) => Id == other.Id;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is TweenHandle other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>Determines whether two handles are equal.</summary>
        public static bool operator ==(TweenHandle left, TweenHandle right) => left.Equals(right);

        /// <summary>Determines whether two handles are different.</summary>
        public static bool operator !=(TweenHandle left, TweenHandle right) => !left.Equals(right);
    }
}
