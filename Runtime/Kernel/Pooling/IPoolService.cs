namespace Tritone.Pooling
{
    /// <summary>
    /// Creates ownership scopes backed by the shared application pool storage.
    /// </summary>
    public interface IPoolService
    {
        /// <summary>
        /// Creates one scope that tracks rented and spawned objects for automatic cleanup.
        /// </summary>
        /// <returns>A new pool ownership scope.</returns>
        IPoolScope CreateScope();
    }
}
