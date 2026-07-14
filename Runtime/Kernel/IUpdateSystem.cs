namespace Tritone.Kernel
{
    /// <summary>
    /// Defines a module or service that receives application frame updates.
    /// </summary>
    public interface IUpdateSystem
    {
        /// <summary>
        /// Gets the update order. Systems with lower values update first.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Updates the system for one application frame.
        /// </summary>
        /// <param name="time">The immutable timing data for the current frame.</param>
        void Update(in FrameTime time);
    }
}
