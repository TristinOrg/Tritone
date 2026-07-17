using System;

namespace Tritone.Audio
{
    /// <summary>
    /// Identifies one active sound effect without exposing its Unity AudioSource.
    /// </summary>
    public readonly struct AudioHandle : IEquatable<AudioHandle>
    {
        // Stores the application-local playback identifier.
        internal readonly int Id;

        /// <summary>
        /// Gets whether this handle can identify a playback.
        /// </summary>
        public bool IsValid => Id > 0;

        /// <summary>
        /// Initializes one immutable audio handle.
        /// </summary>
        /// <param name="id">The positive playback identifier.</param>
        internal AudioHandle(int id)
        {
            Id = id;
        }

        /// <inheritdoc />
        public bool Equals(AudioHandle other)
        {
            return Id == other.Id;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is AudioHandle other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Id;
        }
    }
}
