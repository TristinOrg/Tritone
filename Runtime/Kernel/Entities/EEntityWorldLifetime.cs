namespace Tritone.Entities
{
    /// <summary>
    /// Defines the application boundary that owns one entity world.
    /// </summary>
    public enum EEntityWorldLifetime
    {
        /// <summary>
        /// Keeps the world alive until the application stops.
        /// </summary>
        Application,

        /// <summary>
        /// Recreates the world for every active scene module.
        /// </summary>
        Scene
    }
}
