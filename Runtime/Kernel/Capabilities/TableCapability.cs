using System.Threading.Tasks;
using Tritone.Tables;

namespace Tritone.Kernel
{

    /// <summary>
    /// Provides configuration table operations whose ownership follows one module context.
    /// </summary>
    public sealed class TableCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific table scope.
        private ITableScope mScope;

        /// <summary>
        /// Initializes table operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal TableCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Loads, indexes, and owns one strongly typed table.
        /// </summary>
        /// <typeparam name="TKey">The stable row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="path">The asset-provider table path.</param>
        /// <returns>The loaded configuration table.</returns>
        public Table<TKey, TRow> Load<TKey, TRow>(string path)
            where TRow : ITableRow<TKey>
        {
            return GetScope().Load<TKey, TRow>(path);
        }

        /// <summary>
        /// Loads, indexes, and owns one strongly typed table asynchronously.
        /// </summary>
        /// <typeparam name="TKey">The stable row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="path">The asset-provider table path.</param>
        /// <returns>A task containing the loaded configuration table.</returns>
        public Task<Table<TKey, TRow>> LoadAsync<TKey, TRow>(string path)
            where TRow : ITableRow<TKey>
        {
            return GetScope().LoadAsync<TKey, TRow>(path);
        }

        /// <summary>
        /// Releases one table before the module lifetime ends.
        /// </summary>
        /// <typeparam name="TKey">The stable row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="table">The loaded table to release.</param>
        /// <returns>True when this capability owned the table; otherwise, false.</returns>
        public bool Release<TKey, TRow>(Table<TKey, TRow> table)
            where TRow : ITableRow<TKey>
        {
            return mScope != null && mScope.Release(table);
        }

        /// <summary>
        /// Gets or creates the table scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned table scope.</returns>
        private ITableScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<ITableService>(
                "Table infrastructure is not configured. Call builder.UseTables() before adding game modules.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }
    }
}
