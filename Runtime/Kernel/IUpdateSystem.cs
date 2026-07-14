namespace Tritone.Kernel
{
    /// <summary>
    /// Defines a module or service that receives application frame updates.
    /// </summary>
    public interface IUpdateSystem : IOrderedUpdateSystem
    {
        /// <summary>
        /// Updates the system for one application frame.
        /// </summary>
        /// <param name="time">The immutable timing data for the current frame.</param>
        void Update(in FrameTime time);
    }
}
