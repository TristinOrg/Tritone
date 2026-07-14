namespace Tritone.Kernel
{
    /// <summary>
    /// Defines a system that updates after the normal application update stage.
    /// </summary>
    public interface ILateUpdateSystem : IOrderedUpdateSystem
    {
        /// <summary>
        /// Updates the system after the normal application update stage.
        /// </summary>
        /// <param name="time">The timing data for the current frame.</param>
        void LateUpdate(in FrameTime time);
    }
}
