using System;
using System.Threading.Tasks;

namespace Tritone.Assets
{
    /// <summary>
    /// Owns asset references acquired by one module or Unity component.
    /// </summary>
    public interface IAssetScope : IDisposable
    {
        /// <summary>
        /// Loads and owns one asset synchronously.
        /// </summary>
        /// <typeparam name="T">The requested reference type.</typeparam>
        /// <param name="path">The provider-specific asset path.</param>
        /// <returns>The loaded asset.</returns>
        T Load<T>(string path) where T : class;

        /// <summary>
        /// Loads and owns one asset asynchronously.
        /// </summary>
        /// <typeparam name="T">The requested reference type.</typeparam>
        /// <param name="path">The provider-specific asset path.</param>
        /// <returns>A task containing the loaded asset.</returns>
        Task<T> LoadAsync<T>(string path) where T : class;

        /// <summary>
        /// Releases one asset reference previously acquired by this scope.
        /// </summary>
        /// <typeparam name="T">The loaded asset type.</typeparam>
        /// <param name="asset">The loaded asset to release.</param>
        /// <returns>True when this scope owned a matching reference; otherwise, false.</returns>
        bool Release<T>(T asset) where T : class;
    }
}
