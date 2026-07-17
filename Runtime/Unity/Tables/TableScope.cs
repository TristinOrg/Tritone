using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tritone.Tables;

namespace Tritone.Unity.Tables
{
    /// <summary>
    /// Tracks table references so its owner can release them as one lifetime.
    /// </summary>
    internal sealed class TableScope : ITableScope
    {
        // Stores the shared table module.
        private readonly TableModule mModule;

        // Stores one lightweight record for every acquired table reference.
        private readonly List<TableLease> mLeases = new();

        // Indicates whether this scope has released its references.
        private bool mDisposed;

        /// <summary>
        /// Initializes one empty table ownership scope.
        /// </summary>
        /// <param name="module">The shared table module.</param>
        internal TableScope(TableModule module)
        {
            mModule = module;
        }

        /// <inheritdoc />
        public Table<TKey, TRow> Load<TKey, TRow>(string path) where TRow : ITableRow<TKey>
        {
            ThrowIfDisposed();
            return mModule.Load<TKey, TRow>(this, path);
        }

        /// <inheritdoc />
        public Task<Table<TKey, TRow>> LoadAsync<TKey, TRow>(string path)
            where TRow : ITableRow<TKey>
        {
            ThrowIfDisposed();
            return mModule.LoadAsync<TKey, TRow>(this, path);
        }

        /// <inheritdoc />
        public bool Release<TKey, TRow>(Table<TKey, TRow> table) where TRow : ITableRow<TKey>
        {
            if (mDisposed || table == null)
                return false;

            for (int i = mLeases.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(mLeases[i].Table, table))
                    continue;

                var key       = mLeases[i].Key;
                var lastIndex = mLeases.Count - 1;
                mLeases[i]    = mLeases[lastIndex];
                mLeases.RemoveAt(lastIndex);
                mModule.Release(key);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (mDisposed)
                return;

            mDisposed = true;
            for (int i = mLeases.Count - 1; i >= 0; i--)
                mModule.Release(mLeases[i].Key);
            mLeases.Clear();
        }

        /// <summary>
        /// Records one acquired reference while this scope remains active.
        /// </summary>
        /// <param name="key">The shared table cache key.</param>
        /// <param name="table">The table returned to this scope.</param>
        /// <returns>True when the reference was recorded; otherwise, false.</returns>
        internal bool Track(TableKey key, object table)
        {
            if (mDisposed)
                return false;

            mLeases.Add(new TableLease(key, table));
            return true;
        }

        /// <summary>
        /// Rejects access after this scope has been released.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (mDisposed)
                throw new ObjectDisposedException(nameof(TableScope));
        }
    }

    /// <summary>
    /// Stores one table reference acquired by a table scope.
    /// </summary>
    internal readonly struct TableLease
    {
        // Stores the cache key whose reference count must be released.
        internal readonly TableKey Key;

        // Stores the table reference used by the convenient release call.
        internal readonly object Table;

        /// <summary>
        /// Initializes one immutable table lease.
        /// </summary>
        /// <param name="key">The shared table cache key.</param>
        /// <param name="table">The acquired table reference.</param>
        internal TableLease(TableKey key, object table)
        {
            Key   = key;
            Table = table;
        }
    }
}
