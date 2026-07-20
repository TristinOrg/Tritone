using Tritone.Kernel;

namespace Tritone.Entities
{
    /// <summary>
    /// Defines deterministic logic that updates one entity world.
    /// </summary>
    public interface IEntitySystem : IOrderedUpdateSystem
    {
        /// <summary>
        /// Initializes the system for one newly created world.
        /// </summary>
        /// <param name="world">The world owned by this system lifetime.</param>
        void Initialize(EntityWorld world);

        /// <summary>
        /// Updates entity state for one application frame.
        /// </summary>
        /// <param name="time">The immutable timing data for the current frame.</param>
        void Update(in FrameTime time);

        /// <summary>
        /// Releases system state before its world is cleared.
        /// </summary>
        void Shutdown();
    }
}
