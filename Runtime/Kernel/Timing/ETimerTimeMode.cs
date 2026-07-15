namespace Tritone.Timing
{
    /// <summary>
    /// Defines which application clock advances a timer.
    /// </summary>
    public enum ETimerTimeMode
    {
        /// <summary>
        /// Uses scaled time affected by the host time scale.
        /// </summary>
        Scaled,

        /// <summary>
        /// Uses unscaled time unaffected by the host time scale.
        /// </summary>
        Unscaled
    }
}
