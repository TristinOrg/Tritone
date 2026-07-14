namespace Tritone.Kernel
{
    /// <summary>
    /// Contains the timing values passed to every update system for one frame.
    /// </summary>
    public readonly struct FrameTime
    {
        /// <summary>
        /// Initializes timing data for one frame.
        /// </summary>
        /// <param name="deltaTime">The scaled time in seconds since the previous frame.</param>
        /// <param name="unscaledDeltaTime">The unscaled time in seconds since the previous frame.</param>
        /// <param name="elapsedTime">The total unscaled time in seconds since application startup.</param>
        /// <param name="frameIndex">The zero-based index of the current frame.</param>
        public FrameTime(double deltaTime, double unscaledDeltaTime, double elapsedTime, ulong frameIndex)
        {
            DeltaTime         = deltaTime;
            UnscaledDeltaTime = unscaledDeltaTime;
            ElapsedTime       = elapsedTime;
            FrameIndex        = frameIndex;
        }

        /// <summary>
        /// Gets the scaled time in seconds since the previous frame.
        /// </summary>
        public double DeltaTime { get; }

        /// <summary>
        /// Gets the unscaled time in seconds since the previous frame.
        /// </summary>
        public double UnscaledDeltaTime { get; }

        /// <summary>
        /// Gets the total unscaled time in seconds since application startup.
        /// </summary>
        public double ElapsedTime { get; }

        /// <summary>
        /// Gets the zero-based index of the current frame.
        /// </summary>
        public ulong FrameIndex { get; }
    }
}
