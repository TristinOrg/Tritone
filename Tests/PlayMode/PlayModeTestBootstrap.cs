using Tritone.Kernel;
using Tritone.Unity;

namespace Tritone.PlayMode.Tests
{
    /// <summary>
    /// Configures the smallest real Unity bootstrap used by package smoke tests.
    /// </summary>
    public sealed class PlayModeTestBootstrap : TritoneBootstrap
    {
        /// <summary>
        /// Gets the update probe registered during bootstrap configuration.
        /// </summary>
        public PlayModeProbeModule Probe { get; private set; }

        /// <inheritdoc />
        protected override void Configure(GameApplicationBuilder builder)
        {
            Probe = new PlayModeProbeModule();
            builder.AddModule(Probe);
        }
    }
}
