using System;
using System.Collections.Generic;

namespace Tritone.Tables
{
    /// <summary>
    /// Stores immutable configuration rows and provides constant-time primary-key lookup.
    /// </summary>
    /// <typeparam name="TKey">The row key type.</typeparam>
    /// <typeparam name="TRow">The configuration row type.</typeparam>
    public sealed class Table<TKey, TRow> where TRow : ITableRow<TKey>
    {
        // Stores rows in their source order for allocation-free indexed traversal.
        private readonly TRow[] mRows;

        // Maps stable primary keys to rows.
        private readonly Dictionary<TKey, TRow> mRowsByKey;

        /// <summary>
        /// Gets the number of rows in this table.
        /// </summary>
        public int Count => mRows.Length;

        /// <summary>
        /// Initializes and indexes one complete row array.
        /// </summary>
        /// <param name="rows">The rows whose ownership is transferred to this table.</param>
        public Table(TRow[] rows)
        {
            mRows      = rows ?? throw new ArgumentNullException(nameof(rows));
            mRowsByKey = new Dictionary<TKey, TRow>(rows.Length);

            for (int i = 0, cnt = rows.Length; i < cnt; i++)
            {
                var row = rows[i];
                if (ReferenceEquals(row, null))
                    throw new InvalidOperationException($"Table row at index {i} is null.");

                var key = row.Key;
                if (ReferenceEquals(key, null))
                    throw new InvalidOperationException($"Table row at index {i} has a null key.");
                if (!mRowsByKey.TryAdd(key, row))
                    throw new InvalidOperationException($"Table contains duplicate key '{key}'.");
            }
        }

        /// <summary>
        /// Gets one row by its stable primary key.
        /// </summary>
        /// <param name="key">The primary key to find.</param>
        /// <returns>The matching configuration row.</returns>
        public TRow Get(TKey key)
        {
            if (mRowsByKey.TryGetValue(key, out var row))
                return row;

            throw new KeyNotFoundException($"Table row '{key}' was not found.");
        }

        /// <summary>
        /// Attempts to get one row by its stable primary key.
        /// </summary>
        /// <param name="key">The primary key to find.</param>
        /// <param name="row">The matching row when found; otherwise, the default value.</param>
        /// <returns>True when the row exists; otherwise, false.</returns>
        public bool TryGet(TKey key, out TRow row)
        {
            return mRowsByKey.TryGetValue(key, out row);
        }

        /// <summary>
        /// Gets one row by its source-order index without allocating an enumerator.
        /// </summary>
        /// <param name="index">The zero-based row index.</param>
        /// <returns>The row stored at the requested index.</returns>
        public TRow GetAt(int index)
        {
            return mRows[index];
        }
    }
}
