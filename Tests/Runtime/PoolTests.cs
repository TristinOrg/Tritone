using NUnit.Framework;
using Tritone.Kernel;
using Tritone.Pooling;
using Tritone.Unity.Pooling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies lazy object pools, prefab pools, callbacks, reuse, and scope cleanup.
    /// </summary>
    public sealed class PoolTests
    {
        /// <summary>
        /// Verifies that plain objects are reused without explicit type registration.
        /// </summary>
        [Test]
        public void Rent_Return_ReusesPlainObject()
        {
            var application = CreateApplication(out var consumer);
            var first       = consumer.RentData();
            first.Value     = 42;

            Assert.AreEqual(1, first.SpawnCount);
            Assert.IsTrue(consumer.ReturnData(first));
            Assert.AreEqual(1, first.DespawnCount);

            var second = consumer.RentData();
            Assert.AreSame(first, second);
            Assert.AreEqual(2, second.SpawnCount);
            application.Stop();
        }

        /// <summary>
        /// Verifies that Component prefabs are lazily pooled and reused.
        /// </summary>
        [Test]
        public void Spawn_Despawn_ReusesComponentPrefab()
        {
            var prefabObject = new GameObject("PoolTestPrefab");
            prefabObject.SetActive(false);
            var prefab      = prefabObject.AddComponent<PoolTestComponent>();
            var application = CreateApplication(out var consumer);

            var first = consumer.SpawnComponent(prefab);
            Assert.AreNotSame(prefab, first);
            Assert.IsTrue(first.gameObject.activeSelf);
            Assert.AreEqual(1, first.SpawnCount);

            Assert.IsTrue(consumer.DespawnComponent(first));
            Assert.IsFalse(first.gameObject.activeSelf);
            Assert.AreEqual(1, first.DespawnCount);

            var second = consumer.SpawnComponent(prefab);
            Assert.AreSame(first, second);
            Assert.AreEqual(2, second.SpawnCount);

            application.Stop();
            Object.DestroyImmediate(prefabObject);
        }

        /// <summary>
        /// Verifies that stopping a module returns objects it did not release manually.
        /// </summary>
        [Test]
        public void Stop_AutomaticallyReturnsOwnedObjects()
        {
            var application = CreateApplication(out var consumer);
            var data        = consumer.RentData();

            application.Stop();

            Assert.AreEqual(1, data.DespawnCount);
        }

        /// <summary>
        /// Creates one application containing pool infrastructure and a pool consumer.
        /// </summary>
        private static GameApplication CreateApplication(out PoolConsumerModule consumer)
        {
            consumer = new();
            var application = new GameApplicationBuilder()
                .UsePools()
                .AddModule(consumer)
                .Build();
            application.Start();
            return application;
        }

        /// <summary>
        /// Exposes protected ModuleBase pool helpers for tests.
        /// </summary>
        private sealed class PoolConsumerModule : ModuleBase
        {
            /// <summary>
            /// Rents one test data object.
            /// </summary>
            internal PoolTestData RentData()
            {
                return Rent<PoolTestData>();
            }

            /// <summary>
            /// Returns one test data object.
            /// </summary>
            internal bool ReturnData(PoolTestData instance)
            {
                return Return(instance);
            }

            /// <summary>
            /// Spawns one test Component prefab.
            /// </summary>
            internal PoolTestComponent SpawnComponent(PoolTestComponent prefab)
            {
                return Spawn(prefab);
            }

            /// <summary>
            /// Despawns one test Component instance.
            /// </summary>
            internal bool DespawnComponent(PoolTestComponent instance)
            {
                return Despawn(instance);
            }
        }
    }

    /// <summary>
    /// Provides callback state for plain-object pooling tests.
    /// </summary>
    public sealed class PoolTestData : IPoolable
    {
        // Stores arbitrary state used by the reuse test.
        public int Value;

        // Gets the number of rentals received by this object.
        public int SpawnCount { get; private set; }

        // Gets the number of returns received by this object.
        public int DespawnCount { get; private set; }

        /// <summary>
        /// Records one rental callback.
        /// </summary>
        public void OnSpawn()
        {
            SpawnCount++;
        }

        /// <summary>
        /// Resets state and records one return callback.
        /// </summary>
        public void OnDespawn()
        {
            Value = 0;
            DespawnCount++;
        }
    }

    /// <summary>
    /// Provides callback state for Component prefab pooling tests.
    /// </summary>
    public sealed class PoolTestComponent : MonoBehaviour, IPoolable
    {
        // Gets the number of spawns received by this component.
        public int SpawnCount { get; private set; }

        // Gets the number of despawns received by this component.
        public int DespawnCount { get; private set; }

        /// <summary>
        /// Records one spawn callback.
        /// </summary>
        public void OnSpawn()
        {
            SpawnCount++;
        }

        /// <summary>
        /// Records one despawn callback.
        /// </summary>
        public void OnDespawn()
        {
            DespawnCount++;
        }
    }
}
