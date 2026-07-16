using NUnit.Framework;
using Tritone.Kernel;
using Tritone.Pooling;
using Tritone.Unity;
using Tritone.Unity.Pooling;
using Tritone.Unity.UI;
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
            var returned = first;
            Assert.IsTrue(consumer.ReturnData(ref first));
            Assert.IsNull(first);
            Assert.AreEqual(1, returned.DespawnCount);

            var second = consumer.RentData();
            Assert.AreSame(returned, second);
            Assert.AreEqual(2, second.SpawnCount);
            application.Stop();
        }

        /// <summary>
        /// Verifies that Component prefabs are lazily pooled and reused.
        /// </summary>
        [Test]
        public void Spawn_Despawn_ReusesComponentPrefab()
        {
            GameObject prefabObject = new("PoolTestPrefab");
            prefabObject.SetActive(false);
            var prefab      = prefabObject.AddComponent<PoolTestComponent>();
            var application = CreateApplication(out var consumer);

            var first = consumer.SpawnComponent(prefab);
            Assert.AreNotSame(prefab, first);
            Assert.IsTrue(first.gameObject.activeSelf);
            Assert.AreEqual(1, first.SpawnCount);

            var returned = first;
            Assert.IsTrue(consumer.DespawnComponent(ref first));
            Assert.IsNull(first);
            Assert.IsFalse(returned.gameObject.activeSelf);
            Assert.AreEqual(1, returned.DespawnCount);

            var second = consumer.SpawnComponent(prefab);
            Assert.AreSame(returned, second);
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
        /// Verifies that disabling a UI element returns all objects spawned during its open lifetime.
        /// </summary>
        [Test]
        public void UIElementDisable_AutomaticallyReturnsSpawnedObjects()
        {
            GameObject bootstrapObject = new("PoolTestBootstrap");
            bootstrapObject.SetActive(false);
            var bootstrap = bootstrapObject.AddComponent<PoolTestBootstrap>();
            bootstrap.StartForTest();

            GameObject prefabObject = new("PoolTestUIPrefab");
            prefabObject.SetActive(false);
            var prefab = prefabObject.AddComponent<PoolTestComponent>();

            GameObject elementObject = new("PoolTestUIElement");
            elementObject.SetActive(false);
            elementObject.AddComponent<PoolTestView>();
            var element = elementObject.AddComponent<PoolTestElement>();

            var first = element.SpawnForTest(prefab);
            Assert.IsTrue(first.gameObject.activeSelf);

            element.DisableForTest();
            Assert.IsFalse(first.gameObject.activeSelf);
            Assert.AreEqual(1, first.DespawnCount);

            var second = element.SpawnForTest(prefab);
            Assert.AreSame(first, second);
            element.DisableForTest();

            bootstrap.StopForTest();
            Object.DestroyImmediate(elementObject);
            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(bootstrapObject);
        }

        /// <summary>
        /// Creates one application containing pool infrastructure and a pool consumer.
        /// </summary>
        private static GameApplication CreateApplication(out PoolConsumerModule consumer)
        {
            consumer = new();
            GameApplicationBuilder builder = new();
            var application                = builder.UsePools()
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
            internal bool ReturnData(ref PoolTestData instance)
            {
                return Return(ref instance);
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
            internal bool DespawnComponent(ref PoolTestComponent instance)
            {
                return Despawn(ref instance);
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

    /// <summary>
    /// Creates pool infrastructure for Unity component lifecycle tests.
    /// </summary>
    public sealed class PoolTestBootstrap : TritoneBootstrap
    {
        /// <summary>
        /// Adds the pool module required by the test component.
        /// </summary>
        protected override void Configure(GameApplicationBuilder builder)
        {
            builder.UsePools();
        }

        /// <summary>
        /// Starts the bootstrap explicitly in EditMode.
        /// </summary>
        public void StartForTest()
        {
            base.Awake();
        }

        /// <summary>
        /// Stops the bootstrap explicitly in EditMode.
        /// </summary>
        public void StopForTest()
        {
            base.OnDestroy();
        }
    }

    /// <summary>
    /// Provides an empty typed view for pool lifecycle tests.
    /// </summary>
    public sealed class PoolTestView : UIView
    {
    }

    /// <summary>
    /// Exposes UIElement spawn and disable stages for pool lifecycle tests.
    /// </summary>
    public sealed class PoolTestElement : UIElement<PoolTestView>
    {
        /// <summary>
        /// Spawns one component owned by the current UI enabled lifetime.
        /// </summary>
        public PoolTestComponent SpawnForTest(PoolTestComponent prefab)
        {
            return Spawn(prefab);
        }

        /// <summary>
        /// Runs the UI disable cleanup stage explicitly in EditMode.
        /// </summary>
        public void DisableForTest()
        {
            base.OnDisable();
        }
    }
}
