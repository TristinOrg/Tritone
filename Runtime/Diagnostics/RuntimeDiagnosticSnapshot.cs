using Tritone.Kernel;

namespace Tritone.Diagnostics
{
    /// <summary>
    /// Contains one immutable runtime diagnostic snapshot.
    /// </summary>
    public readonly struct RuntimeDiagnosticSnapshot
    {
        /// <summary>
        /// Initializes one runtime diagnostic snapshot.
        /// </summary>
        /// <param name="applicationState">The application lifecycle state.</param>
        /// <param name="activeModule">The active scene module name.</param>
        /// <param name="activeFlow">The active flow name.</param>
        /// <param name="applicationEntities">The application entity count.</param>
        /// <param name="sceneEntities">The scene entity count.</param>
        /// <param name="framesPerSecond">The sampled frames per second.</param>
        /// <param name="averageFrameMilliseconds">The average frame duration.</param>
        /// <param name="minimumFrameMilliseconds">The minimum frame duration.</param>
        /// <param name="maximumFrameMilliseconds">The maximum frame duration.</param>
        /// <param name="frameIndex">The latest frame index.</param>
        public RuntimeDiagnosticSnapshot(EApplicationState applicationState, string activeModule, string activeFlow, int applicationEntities, int sceneEntities, double framesPerSecond, double averageFrameMilliseconds, double minimumFrameMilliseconds, double maximumFrameMilliseconds, ulong frameIndex)
        {
            ApplicationState         = applicationState;
            ActiveModule             = activeModule;
            ActiveFlow               = activeFlow;
            ApplicationEntities      = applicationEntities;
            SceneEntities            = sceneEntities;
            FramesPerSecond          = framesPerSecond;
            AverageFrameMilliseconds = averageFrameMilliseconds;
            MinimumFrameMilliseconds = minimumFrameMilliseconds;
            MaximumFrameMilliseconds = maximumFrameMilliseconds;
            FrameIndex               = frameIndex;
        }

        /// <summary>Gets the application lifecycle state.</summary>
        public EApplicationState ApplicationState { get; }
        /// <summary>Gets the active scene module name.</summary>
        public string ActiveModule { get; }
        /// <summary>Gets the active flow name.</summary>
        public string ActiveFlow { get; }
        /// <summary>Gets the application entity count.</summary>
        public int ApplicationEntities { get; }
        /// <summary>Gets the active scene entity count.</summary>
        public int SceneEntities { get; }
        /// <summary>Gets the sampled frames per second.</summary>
        public double FramesPerSecond { get; }
        /// <summary>Gets the average sampled frame duration in milliseconds.</summary>
        public double AverageFrameMilliseconds { get; }
        /// <summary>Gets the minimum sampled frame duration in milliseconds.</summary>
        public double MinimumFrameMilliseconds { get; }
        /// <summary>Gets the maximum sampled frame duration in milliseconds.</summary>
        public double MaximumFrameMilliseconds { get; }
        /// <summary>Gets the latest application frame index.</summary>
        public ulong FrameIndex { get; }
    }
}
