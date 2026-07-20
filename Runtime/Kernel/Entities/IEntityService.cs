namespace Tritone.Entities
{
    /// <summary>
    /// Exposes application and active scene entity worlds.
    /// </summary>
    public interface IEntityService
    {
        /// <summary>
        /// Gets the entity world that survives scene changes.
        /// </summary>
        EntityWorld Application { get; }

        /// <summary>
        /// Gets the entity world owned by the active scene module.
        /// </summary>
        EntityWorld Scene { get; }
    }
}
