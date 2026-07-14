namespace Tritone.Kernel
{
    /// <summary>
    /// Defines a system that updates before the normal application update stage.
    /// </summary>
    public interface IPreUpdateSystem : IOrderedUpdateSystem
    {
        /// <summary>
        /// Updates the system before the normal application update stage.
        /// </summary>
        /// <param name="time">The timing data for the current frame.</param>
        void PreUpdate(in FrameTime time);
    }
}
