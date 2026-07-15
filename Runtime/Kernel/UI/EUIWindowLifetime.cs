namespace Tritone.UI
{
    /// <summary>
    /// Defines whether a window is globally available or owned by active modules.
    /// </summary>
    public enum EUIWindowLifetime
    {
        /// <summary>
        /// Keeps the window available for the complete application lifetime.
        /// </summary>
        Application,

        /// <summary>
        /// Makes the window available only while at least one owning module is active.
        /// </summary>
        Module
    }
}
