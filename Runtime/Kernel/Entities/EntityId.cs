using System;

namespace Tritone.Entities
{
    /// <summary>
    /// Identifies one entity slot and the generation that currently owns it.
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>
    {
        /// <summary>
        /// Initializes one entity identifier.
        /// </summary>
        /// <param name="index">The zero-based world slot index.</param>
        /// <param name="generation">The positive slot generation.</param>
        internal EntityId(int index, int generation)
        {
            Index      = index;
            Generation = generation;
        }

        /// <summary>
        /// Gets the zero-based world slot index.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the generation that prevents stale identifier reuse.
        /// </summary>
        public int Generation { get; }

        /// <inheritdoc />
        public bool Equals(EntityId other)
        {
            return Index == other.Index && Generation == other.Generation;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is EntityId other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Index * 397) ^ Generation;
            }
        }

        /// <summary>
        /// Determines whether two identifiers represent the same entity generation.
        /// </summary>
        public static bool operator ==(EntityId left, EntityId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two identifiers represent different entity generations.
        /// </summary>
        public static bool operator !=(EntityId left, EntityId right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Entity({Index}:{Generation})";
        }
    }
}
