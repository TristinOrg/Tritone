using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Assets;
using Tritone.Kernel;
using Tritone.Tables;
using Tritone.Unity.Assets;
using Tritone.Unity.Tables;
using UnityEngine;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies typed table indexing, shared loading, and automatic source-asset release.
    /// </summary>
    public sealed class TableTests
    {
        // Stores deterministic role table JSON used by loading tests.
        private const string RoleJson =
            "{\"Rows\":[{\"Id\":1001,\"Name\":\"Tristin\"},{\"Id\":1002,\"Name\":\"Aigis\"}]}";

        /// <summary>
        /// Verifies constant-time key lookup and source-order access.
        /// </summary>
        [Test]
        public void Table_IndexesRowsByStableKey()
        {
            var first = new RoleRow(1001, "Tristin");
            var second = new RoleRow(1002, "Aigis");
            Table<int, RoleRow> table = new(new[] { first, second });

            Assert.AreEqual(2, table.Count);
            Assert.AreSame(first, table.Get(1001));
            Assert.AreSame(second, table.GetAt(1));
            Assert.IsTrue(table.TryGet(1002, out var found));
            Assert.AreSame(second, found);
            Assert.IsFalse(table.TryGet(9999, out _));
        }

        /// <summary>
        /// Verifies that duplicate primary keys are rejected during one-time indexing.
        /// </summary>
        [Test]
        public void Table_DuplicateKeyThrows()
        {
            var rows = new[]
            {
                new RoleRow(1001, "First"),
                new RoleRow(1001, "Second")
            };

            Assert.Throws<InvalidOperationException>(() => new Table<int, RoleRow>(rows));
        }

        /// <summary>
        /// Verifies that failed indexing immediately releases its loaded source asset.
        /// </summary>
        [Test]
        public void LoadTable_DuplicateKeysReleaseSourceAsset()
        {
            const string duplicateJson =
                "{\"Rows\":[{\"Id\":1001,\"Name\":\"First\"},{\"Id\":1001,\"Name\":\"Second\"}]}";
            TableAssetProvider provider = new(duplicateJson);
            var application             = CreateApplication(provider, out var consumer);

            Assert.Throws<InvalidOperationException>(() => consumer.LoadRoles());
            Assert.AreEqual(1, provider.ReleaseCount);

            application.Stop();
            Assert.AreEqual(1, provider.ReleaseCount);
        }

        /// <summary>
        /// Verifies Unity JSON loading and shared parsed-table reuse.
        /// </summary>
        [Test]
        public void LoadTable_ReusesParsedTableUntilFinalReferenceIsReleased()
        {
            TableAssetProvider provider = new(RoleJson);
            var application             = CreateApplication(provider, out var consumer);

            var first  = consumer.LoadRoles();
            var second = consumer.LoadRoles();

            Assert.AreSame(first, second);
            Assert.AreEqual("Aigis", first.Get(1002).Name);
            Assert.AreEqual(1, provider.LoadCount);
            Assert.IsTrue(consumer.ReleaseRoles(first));
            Assert.AreEqual(0, provider.ReleaseCount);

            application.Stop();
            Assert.AreEqual(1, provider.ReleaseCount);
        }

        /// <summary>
        /// Verifies that concurrent asynchronous callers share asset loading and table parsing.
        /// </summary>
        [Test]
        public void LoadTableAsync_MergesConcurrentRequests()
        {
            TableAssetProvider provider = new(RoleJson, true);
            var application             = CreateApplication(provider, out var consumer);

            var firstTask  = consumer.LoadRolesAsync();
            var secondTask = consumer.LoadRolesAsync();
            Assert.AreEqual(1, provider.LoadAsyncCount);

            provider.CompleteAsync();
            var first  = firstTask.GetAwaiter().GetResult();
            var second = secondTask.GetAwaiter().GetResult();

            Assert.AreSame(first, second);
            Assert.AreEqual(2, first.Count);
            application.Stop();
            Assert.AreEqual(1, provider.ReleaseCount);
        }

        /// <summary>
        /// Verifies that the ref release overload clears the caller and unloads the final source.
        /// </summary>
        [Test]
        public void ReleaseTable_FinalReferenceClearsCallerAndReleasesSource()
        {
            TableAssetProvider provider = new(RoleJson);
            var application             = CreateApplication(provider, out var consumer);
            var table                   = consumer.LoadRoles();

            Assert.IsTrue(consumer.ReleaseRoles(ref table));
            Assert.IsNull(table);
            Assert.AreEqual(1, provider.ReleaseCount);

            application.Stop();
            Assert.AreEqual(1, provider.ReleaseCount);
        }

        /// <summary>
        /// Verifies that module shutdown releases tables without manual cleanup.
        /// </summary>
        [Test]
        public void Stop_AutomaticallyReleasesOwnedTables()
        {
            TableAssetProvider provider = new(RoleJson);
            var application             = CreateApplication(provider, out var consumer);

            consumer.LoadRoles();
            application.Stop();

            Assert.AreEqual(1, provider.ReleaseCount);
        }

        /// <summary>
        /// Creates one application containing asset, table, and consumer modules.
        /// </summary>
        /// <param name="provider">The deterministic source asset provider.</param>
        /// <param name="consumer">The configured table consumer.</param>
        /// <returns>The started test application.</returns>
        private static GameApplication CreateApplication(TableAssetProvider provider,
                                                         out TableConsumerModule consumer)
        {
            consumer = new();
            GameApplicationBuilder builder = new();
            var application                = builder.UseAssets(provider)
                .UseTables()
                .AddModule(consumer)
                .Build();
            application.Start();
            return application;
        }

        /// <summary>
        /// Exposes protected ModuleBase table helpers for tests.
        /// </summary>
        private sealed class TableConsumerModule : ModuleBase
        {
            /// <summary>
            /// Loads the role table synchronously.
            /// </summary>
            /// <returns>The loaded role table.</returns>
            internal Table<int, RoleRow> LoadRoles()
            {
                return LoadTable<int, RoleRow>("Tables/Roles");
            }

            /// <summary>
            /// Loads the role table asynchronously.
            /// </summary>
            /// <returns>A task containing the loaded role table.</returns>
            internal Task<Table<int, RoleRow>> LoadRolesAsync()
            {
                return LoadTableAsync<int, RoleRow>("Tables/Roles");
            }

            /// <summary>
            /// Releases one role table reference.
            /// </summary>
            /// <param name="table">The table to release.</param>
            /// <returns>True when the module owned the reference; otherwise, false.</returns>
            internal bool ReleaseRoles(Table<int, RoleRow> table)
            {
                return ReleaseTable(table);
            }

            /// <summary>
            /// Releases one role table reference and clears it.
            /// </summary>
            /// <param name="table">The table reference to release and clear.</param>
            /// <returns>True when the module owned the reference; otherwise, false.</returns>
            internal bool ReleaseRoles(ref Table<int, RoleRow> table)
            {
                return ReleaseTable(ref table);
            }
        }

        /// <summary>
        /// Provides one deterministic table TextAsset with optional delayed async completion.
        /// </summary>
        private sealed class TableAssetProvider : IAssetProvider
        {
            // Stores the stable Unity source asset.
            private readonly TextAsset mAsset;

            // Delays asynchronous loading until explicitly completed.
            private readonly bool mDelayAsync;

            // Stores one delayed asynchronous request.
            private TaskCompletionSource<object> mPendingLoad;

            // Gets the number of synchronous source loads.
            internal int LoadCount { get; private set; }

            // Gets the number of asynchronous source loads.
            internal int LoadAsyncCount { get; private set; }

            // Gets the number of final source releases.
            internal int ReleaseCount { get; private set; }

            /// <summary>
            /// Initializes one provider from deterministic JSON.
            /// </summary>
            /// <param name="json">The complete table JSON.</param>
            /// <param name="delayAsync">Whether async loading requires explicit completion.</param>
            internal TableAssetProvider(string json, bool delayAsync = false)
            {
                mAsset      = new TextAsset(json);
                mDelayAsync = delayAsync;
            }

            /// <inheritdoc />
            public object Load(string path, Type assetType)
            {
                LoadCount++;
                return mAsset;
            }

            /// <inheritdoc />
            public Task<object> LoadAsync(string path, Type assetType)
            {
                LoadAsyncCount++;
                if (!mDelayAsync)
                    return Task.FromResult((object)mAsset);

                mPendingLoad = new();
                return mPendingLoad.Task;
            }

            /// <inheritdoc />
            public void Release(object asset)
            {
                ReleaseCount++;
            }

            /// <summary>
            /// Completes the delayed asynchronous source request.
            /// </summary>
            internal void CompleteAsync()
            {
                var pending = mPendingLoad;
                mPendingLoad = null;
                pending.SetResult(mAsset);
            }
        }
    }

    /// <summary>
    /// Stores one role configuration row used by table tests.
    /// </summary>
    [Serializable]
    public sealed class RoleRow : ITableRow<int>
    {
        // Stores the serialized role identifier.
        public int Id;

        // Stores the serialized display name.
        public string Name;

        /// <summary>
        /// Gets the stable primary key used by the table index.
        /// </summary>
        public int Key => Id;

        /// <summary>
        /// Initializes an empty row for Unity JSON deserialization.
        /// </summary>
        public RoleRow() { }

        /// <summary>
        /// Initializes one deterministic role row.
        /// </summary>
        /// <param name="id">The stable role identifier.</param>
        /// <param name="name">The role display name.</param>
        public RoleRow(int id, string name)
        {
            Id   = id;
            Name = name;
        }
    }
}
