using System;

namespace Tritone.Timing
{
    /// <summary>
    /// Identifies one timer owned by a module timer scope.
    /// </summary>
    public readonly struct TimerHandle : IEquatable<TimerHandle>
    {
        /// <summary>
        /// Initializes a timer handle with a non-zero identifier.
        /// </summary>
        /// <param name="id">The unique timer identifier.</param>
        internal TimerHandle(ulong id)
        {
            Id = id;
        }

        /// <summary>
        /// Gets an invalid timer handle.
        /// </summary>
        public static TimerHandle Invalid => default;

        /// <summary>
        /// Gets whether this handle contains a valid identifier.
        /// </summary>
        public bool IsValid => Id != 0;

        /// <summary>
        /// Gets the unique timer identifier.
        /// </summary>
        internal ulong Id { get; }

        /// <summary>
        /// Determines whether this handle equals another handle.
        /// </summary>
        /// <param name="other">The other handle to compare.</param>
        /// <returns>True when both handles identify the same timer; otherwise, false.</returns>
        public bool Equals(TimerHandle other)
        {
            return Id == other.Id;
        }

        /// <summary>
        /// Determines whether this handle equals another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True when the object is an equal timer handle; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is TimerHandle other && Equals(other);
        }

        /// <summary>
        /// Gets a hash code based on the timer identifier.
        /// </summary>
        /// <returns>The timer identifier hash code.</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Determines whether two handles identify the same timer.
        /// </summary>
        /// <param name="left">The left handle.</param>
        /// <param name="right">The right handle.</param>
        /// <returns>True when both handles are equal; otherwise, false.</returns>
        public static bool operator ==(TimerHandle left, TimerHandle right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two handles identify different timers.
        /// </summary>
        /// <param name="left">The left handle.</param>
        /// <param name="right">The right handle.</param>
        /// <returns>True when both handles are different; otherwise, false.</returns>
        public static bool operator !=(TimerHandle left, TimerHandle right)
        {
            return !left.Equals(right);
        }
    }
}
