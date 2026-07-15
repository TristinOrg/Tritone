namespace Tritone.Timing
{
    /// <summary>
    /// Creates timer lifetime scopes for application modules.
    /// </summary>
    public interface ITimerService
    {
        /// <summary>
        /// Creates a timer scope that owns and automatically cleans up multiple timers.
        /// </summary>
        /// <returns>A new timer lifetime scope.</returns>
        ITimerScope CreateScope();
    }
}
