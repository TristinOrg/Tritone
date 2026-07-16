namespace Tritone.Pooling
{
    /// <summary>
    /// Provides optional callbacks for objects managed by a Tritone pool.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Invoked whenever the object is rented or spawned.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Invoked before the object returns to its pool.
        /// </summary>
        void OnDespawn();
    }
}
