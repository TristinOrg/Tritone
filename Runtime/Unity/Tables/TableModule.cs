using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tritone.Kernel;
using Tritone.Tables;
using UnityEngine;

namespace Tritone.Unity.Tables
{
    /// <summary>
    /// Loads, indexes, shares, and releases strongly typed configuration tables.
    /// </summary>
    public sealed class TableModule : ModuleBase, ITableService
    {
        // Converts configuration bytes into typed row arrays.
        private readonly ITableDeserializer mDeserializer;

        // Maps each path and type combination to its shared table state.
        private readonly Dictionary<TableKey, TableEntry> mEntries = new();

        // Indicates whether this module has permanently stopped.
        private bool mStopped;

        /// <summary>
        /// Initializes table management with one replaceable data deserializer.
        /// </summary>
        /// <param name="deserializer">The table data deserializer.</param>
        public TableModule(ITableDeserializer deserializer)
        {
            mDeserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        }

        /// <summary>
        /// Registers application-wide table access.
        /// </summary>
        /// <param name="services">The application service registry.</param>
        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton<ITableService>(this);
        }

        /// <inheritdoc />
        public ITableScope CreateScope()
        {
            if (mStopped)
                throw new InvalidOperationException(
                    "Table scopes cannot be created after the table module has stopped.");

            return new TableScope(this);
        }

        /// <summary>
        /// Loads or reuses one table for a specific ownership scope.
        /// </summary>
        /// <typeparam name="TKey">The row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="owner">The scope receiving table ownership.</param>
        /// <param name="path">The configuration asset path.</param>
        /// <returns>The loaded and indexed table.</returns>
        internal Table<TKey, TRow> Load<TKey, TRow>(TableScope owner, string path)
            where TRow : ITableRow<TKey>
        {
            ValidatePath(path);
            ThrowIfStopped();

            TableKey key = new(path, typeof(TKey), typeof(TRow));
            if (!mEntries.TryGetValue(key, out var entry))
            {
                entry = new();
                mEntries.Add(key, entry);
            }
            else if (entry.Table != null)
                return Acquire(owner, key, entry, (Table<TKey, TRow>)entry.Table);
            else if (entry.LoadTask != null)
                throw new InvalidOperationException(
                    $"Table '{path}' is already loading asynchronously and cannot be loaded synchronously.");

            TextAsset source = null;
            try
            {
                source            = LoadAsset<TextAsset>(path);
                var rows          = mDeserializer.Deserialize<TRow>(source.bytes);
                var table         = new Table<TKey, TRow>(rows ?? Array.Empty<TRow>());
                entry.SourceAsset = source;
                entry.Table       = table;
                return Acquire(owner, key, entry, table);
            }
            catch
            {
                if (source != null)
                    ReleaseAsset(source);
                mEntries.Remove(key);
                throw;
            }
        }

        /// <summary>
        /// Loads or joins one asynchronous table request for a specific ownership scope.
        /// </summary>
        /// <typeparam name="TKey">The row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="owner">The scope receiving table ownership.</param>
        /// <param name="path">The configuration asset path.</param>
        /// <returns>A task containing the loaded and indexed table.</returns>
        internal async Task<Table<TKey, TRow>> LoadAsync<TKey, TRow>(TableScope owner,
                                                                     string path)
            where TRow : ITableRow<TKey>
        {
            ValidatePath(path);
            ThrowIfStopped();

            TableKey key = new(path, typeof(TKey), typeof(TRow));
            if (!mEntries.TryGetValue(key, out var entry))
            {
                entry = new();
                mEntries.Add(key, entry);
            }
            if (entry.Table != null)
                return Acquire(owner, key, entry, (Table<TKey, TRow>)entry.Table);

            entry.PendingCount++;
            try
            {
                entry.LoadTask ??= LoadAndCreateAsync<TKey, TRow>(entry, path);
                var table = (Table<TKey, TRow>)await entry.LoadTask;
                if (mStopped)
                    throw new ObjectDisposedException(nameof(TableModule));

                if (entry.Table == null)
                {
                    entry.Table    = table;
                    entry.LoadTask = null;
                }
                else if (!ReferenceEquals(entry.Table, table))
                    throw new InvalidOperationException(
                        $"The shared table request '{path}' produced different instances.");

                return Acquire(owner, key, entry, (Table<TKey, TRow>)entry.Table);
            }
            finally
            {
                entry.PendingCount--;
                if (!mStopped)
                    TryReleaseUnused(key, entry);
            }
        }

        /// <summary>
        /// Releases one shared table reference and its source asset after the final owner leaves.
        /// </summary>
        /// <param name="key">The shared table cache key.</param>
        internal void Release(TableKey key)
        {
            if (!mEntries.TryGetValue(key, out var entry) || entry.ReferenceCount < 1)
                return;

            entry.ReferenceCount--;
            TryReleaseUnused(key, entry);
        }

        /// <summary>
        /// Prevents new scopes and lets ModuleBase release every remaining source asset.
        /// </summary>
        protected override void OnStop()
        {
            mStopped = true;
            mEntries.Clear();
        }

        /// <summary>
        /// Loads and indexes one source asset for a shared asynchronous request.
        /// </summary>
        /// <typeparam name="TKey">The row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="entry">The shared table entry.</param>
        /// <param name="path">The configuration asset path.</param>
        /// <returns>A task containing the indexed table.</returns>
        private async Task<object> LoadAndCreateAsync<TKey, TRow>(TableEntry entry, string path)
            where TRow : ITableRow<TKey>
        {
            TextAsset source = null;
            try
            {
                source    = await LoadAssetAsync<TextAsset>(path);
                var rows  = mDeserializer.Deserialize<TRow>(source.bytes);
                var table = new Table<TKey, TRow>(rows ?? Array.Empty<TRow>());
                entry.SourceAsset = source;
                return table;
            }
            catch
            {
                if (source != null)
                    ReleaseAsset(source);
                throw;
            }
        }

        /// <summary>
        /// Adds one shared reference and records it in the owning scope.
        /// </summary>
        /// <typeparam name="TKey">The row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="owner">The scope receiving ownership.</param>
        /// <param name="key">The shared table cache key.</param>
        /// <param name="entry">The shared table entry.</param>
        /// <param name="table">The table returned to the caller.</param>
        /// <returns>The same table instance.</returns>
        private Table<TKey, TRow> Acquire<TKey, TRow>(TableScope owner,
                                                       TableKey key,
                                                       TableEntry entry,
                                                       Table<TKey, TRow> table)
            where TRow : ITableRow<TKey>
        {
            entry.ReferenceCount++;
            if (owner.Track(key, table))
                return table;

            Release(key);
            throw new ObjectDisposedException(nameof(TableScope));
        }

        /// <summary>
        /// Removes one unused entry and releases its source asset.
        /// </summary>
        /// <param name="key">The shared table cache key.</param>
        /// <param name="entry">The candidate shared entry.</param>
        private void TryReleaseUnused(TableKey key, TableEntry entry)
        {
            if (entry.ReferenceCount > 0 || entry.PendingCount > 0)
                return;
            if (!mEntries.TryGetValue(key, out var current) || !ReferenceEquals(current, entry))
                return;

            mEntries.Remove(key);
            if (entry.SourceAsset != null)
            {
                ReleaseAsset(entry.SourceAsset);
                entry.SourceAsset = null;
            }
        }

        /// <summary>
        /// Rejects empty configuration asset paths.
        /// </summary>
        /// <param name="path">The configuration asset path.</param>
        private static void ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException(
                    "A table path cannot be null, empty, or whitespace.",
                    nameof(path));
        }

        /// <summary>
        /// Rejects requests after application shutdown.
        /// </summary>
        private void ThrowIfStopped()
        {
            if (mStopped)
                throw new ObjectDisposedException(nameof(TableModule));
        }
    }

    /// <summary>
    /// Stores the shared load and ownership state for one configuration table.
    /// </summary>
    internal sealed class TableEntry
    {
        // Stores the completed indexed table.
        internal object Table;

        // Stores the source text asset retained while the table has owners.
        internal TextAsset SourceAsset;

        // Stores one in-flight request shared by concurrent callers.
        internal Task<object> LoadTask;

        // Stores the number of active scope references.
        internal int ReferenceCount;

        // Stores the number of callers awaiting the shared request.
        internal int PendingCount;
    }
}
