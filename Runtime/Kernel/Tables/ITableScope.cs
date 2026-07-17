using System;
using System.Threading.Tasks;

namespace Tritone.Tables
{
    /// <summary>
    /// Owns configuration table references acquired by one framework consumer.
    /// </summary>
    public interface ITableScope : IDisposable
    {
        /// <summary>
        /// Loads and owns one strongly typed configuration table.
        /// </summary>
        /// <typeparam name="TKey">The row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="path">The asset-provider path of the configuration file.</param>
        /// <returns>The loaded and indexed table.</returns>
        Table<TKey, TRow> Load<TKey, TRow>(string path) where TRow : ITableRow<TKey>;

        /// <summary>
        /// Loads and owns one strongly typed configuration table asynchronously.
        /// </summary>
        /// <typeparam name="TKey">The row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="path">The asset-provider path of the configuration file.</param>
        /// <returns>A task containing the loaded and indexed table.</returns>
        Task<Table<TKey, TRow>> LoadAsync<TKey, TRow>(string path)
            where TRow : ITableRow<TKey>;

        /// <summary>
        /// Releases one table reference previously acquired by this scope.
        /// </summary>
        /// <typeparam name="TKey">The row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="table">The loaded table to release.</param>
        /// <returns>True when this scope owned a matching reference; otherwise, false.</returns>
        bool Release<TKey, TRow>(Table<TKey, TRow> table) where TRow : ITableRow<TKey>;
    }
}
