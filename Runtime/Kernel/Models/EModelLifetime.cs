namespace Tritone.Models
{
    /// <summary>
    /// Defines the lifetime that owns one registered model.
    /// </summary>
    public enum EModelLifetime
    {
        /// <summary>
        /// Keeps the model alive until the application stops.
        /// </summary>
        Application,

        /// <summary>
        /// Keeps the model alive only while one scene module is active.
        /// </summary>
        Scene
    }
}
