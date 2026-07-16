using System;

namespace Tritone.Pooling
{
    /// <summary>
    /// Tracks objects borrowed by one lifetime owner and returns them as a group.
    /// </summary>
    public interface IPoolScope : IDisposable
    {
        /// <summary>
        /// Rents one plain C# object and creates its type pool on first use.
        /// </summary>
        /// <typeparam name="T">The concrete object type.</typeparam>
        /// <returns>A newly created or reused object.</returns>
        T Rent<T>() where T : class, new();

        /// <summary>
        /// Returns one plain C# object owned by this scope.
        /// </summary>
        /// <typeparam name="T">The concrete object type.</typeparam>
        /// <param name="instance">The object to return.</param>
        /// <returns>True when the object was active and returned; otherwise, false.</returns>
        bool Return<T>(T instance) where T : class;

        /// <summary>
        /// Spawns one Unity prefab and creates its prefab pool on first use.
        /// </summary>
        /// <typeparam name="T">The GameObject or Component prefab type.</typeparam>
        /// <param name="prefab">The prefab used as the pool identity.</param>
        /// <param name="parent">The optional Unity Transform parent.</param>
        /// <returns>A newly instantiated or reused prefab object.</returns>
        T Spawn<T>(T prefab, object parent = null) where T : class;

        /// <summary>
        /// Returns one spawned Unity object owned by this scope.
        /// </summary>
        /// <typeparam name="T">The GameObject or Component instance type.</typeparam>
        /// <param name="instance">The spawned object to return.</param>
        /// <returns>True when the object was active and returned; otherwise, false.</returns>
        bool Despawn<T>(T instance) where T : class;
    }
}
