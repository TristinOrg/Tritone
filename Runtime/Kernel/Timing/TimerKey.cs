using System;
using System.Collections.Generic;

namespace Tritone.Timing
{
    /// <summary>
    /// Identifies one module timer with either an integer or ordinal string key.
    /// </summary>
    public readonly struct TimerKey : IEquatable<TimerKey>
    {
        /// <summary>
        /// Identifies an integer-backed timer key.
        /// </summary>
        private const byte IntKind = 1;

        /// <summary>
        /// Identifies a string-backed timer key.
        /// </summary>
        private const byte StringKind = 2;

        /// <summary>
        /// Stores the active key representation.
        /// </summary>
        private readonly byte mKind;

        /// <summary>
        /// Stores the integer key when this value is integer-backed.
        /// </summary>
        private readonly int mIntKey;

        /// <summary>
        /// Stores the string key when this value is string-backed.
        /// </summary>
        private readonly string mStringKey;

        /// <summary>
        /// Initializes an integer-backed timer key without boxing.
        /// </summary>
        /// <param name="key">The caller-defined integer key.</param>
        public TimerKey(int key)
        {
            mKind      = IntKind;
            mIntKey    = key;
            mStringKey = null;
        }

        /// <summary>
        /// Initializes an ordinal string-backed timer key.
        /// </summary>
        /// <param name="key">The non-empty caller-defined string key.</param>
        public TimerKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("A string timer key cannot be null or empty.", nameof(key));

            mKind      = StringKind;
            mIntKey    = 0;
            mStringKey = key;
        }

        /// <summary>
        /// Gets whether this key contains a supported representation.
        /// </summary>
        public bool IsValid => mKind == IntKind || mKind == StringKind;

        /// <summary>
        /// Converts an integer into a timer key without boxing.
        /// </summary>
        /// <param name="key">The caller-defined integer key.</param>
        /// <returns>An integer-backed timer key.</returns>
        public static implicit operator TimerKey(int key)
        {
            return new(key);
        }

        /// <summary>
        /// Converts a string into an ordinal timer key.
        /// </summary>
        /// <param name="key">The caller-defined string key.</param>
        /// <returns>A string-backed timer key.</returns>
        public static implicit operator TimerKey(string key)
        {
            return new(key);
        }

        /// <summary>
        /// Determines whether this key equals another timer key.
        /// </summary>
        /// <param name="other">The other timer key to compare.</param>
        /// <returns>True when both keys have the same representation and value; otherwise, false.</returns>
        public bool Equals(TimerKey other)
        {
            if (mKind != other.mKind)
                return false;
            if (mKind == IntKind)
                return mIntKey == other.mIntKey;
            if (mKind == StringKind)
                return string.Equals(mStringKey, other.mStringKey, StringComparison.Ordinal);
            return true;
        }

        /// <summary>
        /// Determines whether this key equals another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True when the object is an equal timer key; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is TimerKey other && Equals(other);
        }

        /// <summary>
        /// Gets a hash code matching ordinal key equality.
        /// </summary>
        /// <returns>The timer key hash code.</returns>
        public override int GetHashCode()
        {
            if (mKind == IntKind)
                return unchecked((mIntKey * 397) ^ IntKind);
            if (mKind == StringKind)
                return unchecked((StringComparer.Ordinal.GetHashCode(mStringKey) * 397) ^ StringKind);
            return 0;
        }

        /// <summary>
        /// Gets the readable key value used by diagnostics.
        /// </summary>
        /// <returns>The integer or string key representation.</returns>
        public override string ToString()
        {
            if (mKind == IntKind)
                return mIntKey.ToString();
            return mStringKey ?? string.Empty;
        }

        /// <summary>
        /// Determines whether two timer keys are equal.
        /// </summary>
        /// <param name="left">The left timer key.</param>
        /// <param name="right">The right timer key.</param>
        /// <returns>True when both keys are equal; otherwise, false.</returns>
        public static bool operator ==(TimerKey left, TimerKey right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two timer keys are different.
        /// </summary>
        /// <param name="left">The left timer key.</param>
        /// <param name="right">The right timer key.</param>
        /// <returns>True when both keys are different; otherwise, false.</returns>
        public static bool operator !=(TimerKey left, TimerKey right)
        {
            return !left.Equals(right);
        }
    }
}
