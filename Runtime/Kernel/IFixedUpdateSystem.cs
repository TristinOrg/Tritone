namespace Tritone.Kernel
{
    /// <summary>
    /// Defines a system that updates at a fixed simulation interval.
    /// </summary>
    public interface IFixedUpdateSystem : IOrderedUpdateSystem
    {
        /// <summary>
        /// Updates the system at a fixed simulation interval.
        /// </summary>
        /// <param name="time">The timing data for the current fixed update.</param>
        void FixedUpdate(in FrameTime time);
    }
}
