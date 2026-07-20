using Tritone.Settings;

namespace Tritone.Kernel
{

    /// <summary>
    /// Provides access to the configured typed application settings.
    /// </summary>
    public sealed class SettingsCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        /// <summary>
        /// Initializes settings access for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal SettingsCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Gets the configured typed settings service.
        /// </summary>
        public ISettingsService Service =>
            mContext.GetRequired<ISettingsService>(
                "Settings infrastructure is not configured. Call builder.UseSettings() before adding game modules.");
    }
}
