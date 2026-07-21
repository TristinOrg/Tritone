using Tritone.Kernel;

namespace Tritone.PlayMode.Tests
{
    /// <summary>
    /// Records Unity player-loop updates received by one test bootstrap.
    /// </summary>
    public sealed class PlayModeProbeModule : ModuleBase, IUpdateSystem
    {
        /// <inheritdoc />
        public int Order => 0;

        /// <summary>
        /// Gets the number of application updates received by this module.
        /// </summary>
        public int UpdateCount { get; private set; }

        /// <inheritdoc />
        public void Update(in FrameTime time)
        {
            UpdateCount++;
        }
    }
}
