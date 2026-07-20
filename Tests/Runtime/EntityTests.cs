using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tritone.Entities;

namespace Tritone.Kernel.Tests
{
    /// <summary>
    /// Verifies entity generations, dense components, queries, systems, and world lifetimes.
    /// </summary>
    public sealed class EntityTests
    {
        /// <summary>
        /// Verifies destroyed slots are reused with a new generation.
        /// </summary>
        [Test]
        public void CreateAndDestroy_ReusesSlotWithoutRevivingStaleId()
        {
            var application = CreateApplication();
            var world       = application.Services
                                         .GetRequired<IEntityService>()
                                         .Application;

            var first = world.Create();
            Assert.IsTrue(world.IsAlive(first));
            Assert.IsTrue(world.Destroy(first));
            Assert.IsFalse(world.IsAlive(first));

            var second = world.Create();
            Assert.AreEqual(first.Index, second.Index);
            Assert.AreNotEqual(first.Generation, second.Generation);
            Assert.IsFalse(world.Destroy(first));
            application.Stop();
        }

        /// <summary>
        /// Verifies registered struct components support reference mutation and complete cleanup.
        /// </summary>
        [Test]
        public void Components_AddGetRemoveAndDestroyDeterministically()
        {
            var application = CreateApplication();
            var world       = application.Services
                                         .GetRequired<IEntityService>()
                                         .Application;
            var entity = world.Create();

            world.Add(entity, new Position { X = 2.0f, Y = 3.0f });
            ref var position = ref world.Get<Position>(entity);
            position.X = 5.0f;
            Assert.AreEqual(5.0f, world.Get<Position>(entity).X);
            Assert.IsTrue(world.Has<Position>(entity));
            Assert.IsTrue(world.Remove<Position>(entity));
            Assert.IsFalse(world.Has<Position>(entity));
            Assert.IsFalse(world.Remove<Position>(entity));

            world.Add(entity, new Position { X = 8.0f });
            world.Destroy(entity);
            Assert.Throws<InvalidOperationException>(
                () => world.Get<Position>(entity));
            application.Stop();
        }

        /// <summary>
        /// Verifies dense storage grows and one-component queries mutate values without allocations.
        /// </summary>
        [Test]
        public void Query_TraversesDenseComponentsAndGrowsCapacity()
        {
            var application = CreateApplication(2);
            var world       = application.Services
                                         .GetRequired<IEntityService>()
                                         .Application;
            for (var i = 0; i < 20; i++)
            {
                var entity = world.Create();
                world.Add(entity, new Position { X = i });
            }

            var query = world.Query<Position>();
            Assert.AreEqual(20, query.Count);
            for (int i = 0, cnt = query.Count; i < cnt; i++)
            {
                ref var position = ref query.GetComponent(i);
                position.Y = position.X * 2.0f;
                Assert.IsTrue(world.IsAlive(query.GetEntity(i)));
            }
            Assert.AreEqual(38.0f, query.GetComponent(19).Y);
            application.Stop();
        }

        /// <summary>
        /// Verifies two-component queries skip entities missing either component.
        /// </summary>
        [Test]
        public void QueryTwo_FiltersBySparseMembership()
        {
            var application = CreateApplication();
            var world       = application.Services
                                         .GetRequired<IEntityService>()
                                         .Application;
            var first  = world.Create();
            var second = world.Create();
            world.Add(first, new Position { X = 1.0f });
            world.Add(first, new Velocity { X = 4.0f });
            world.Add(second, new Position { X = 2.0f });

            var query = world.Query<Position, Velocity>();
            var matches = 0;
            for (int i = 0, cnt = query.CandidateCount; i < cnt; i++)
            {
                if (!query.TryGetEntity(i, out var entity))
                    continue;
                ref var position = ref query.GetFirst(entity);
                ref var velocity = ref query.GetSecond(entity);
                position.X += velocity.X;
                matches++;
            }

            Assert.AreEqual(1, matches);
            Assert.AreEqual(5.0f, world.Get<Position>(first).X);
            Assert.AreEqual(2.0f, world.Get<Position>(second).X);
            application.Stop();
        }

        /// <summary>
        /// Verifies systems initialize, update, and shut down in stable order.
        /// </summary>
        [Test]
        public void Systems_FollowStableOrderAndUpdateEntities()
        {
            List<string> events = new();
            var application = new GameApplicationBuilder()
                .AddApplicationComponent<Position>()
                .AddApplicationComponent<Velocity>()
                .AddApplicationEntitySystem(
                    () => new MovementSystem(events, 20))
                .AddApplicationEntitySystem(
                    () => new RecordingSystem(events, -10))
                .Build();
            application.Start();
            var world  = application.Services.GetRequired<IEntityService>().Application;
            var entity = world.Create();
            world.Add(entity, new Position());
            world.Add(entity, new Velocity { X = 3.0f });

            FrameTime time = new(1.0, 1.0, 1.0, 0);
            application.Update(in time);
            Assert.AreEqual(3.0f, world.Get<Position>(entity).X);
            application.Stop();

            CollectionAssert.AreEqual(new[]
            {
                "Record.Initialize",
                "Move.Initialize",
                "Record.Update",
                "Move.Update",
                "Move.Shutdown",
                "Record.Shutdown"
            }, events);
        }

        /// <summary>
        /// Verifies application worlds persist while scene worlds are recreated and released.
        /// </summary>
        [Test]
        public void SceneSwitch_RecreatesSceneWorldAndPreservesApplicationWorld()
        {
            var application = new GameApplicationBuilder()
                .AddApplicationComponent<Position>()
                .AddSceneComponent<Position>()
                .AddSceneModule<FirstSceneModule>()
                .AddSceneModule<SecondSceneModule>()
                .Build();
            application.Start();
            var entities = application.Services.GetRequired<IEntityService>();
            var persistentEntity = entities.Application.Create();
            var first = application.SwitchModule<FirstSceneModule>();
            var firstWorld  = first.World;
            var firstEntity = first.Entity;

            var second = application.SwitchModule<SecondSceneModule>();

            Assert.AreSame(entities.Application, second.ApplicationWorld);
            Assert.IsTrue(entities.Application.IsAlive(persistentEntity));
            Assert.AreNotSame(firstWorld, second.World);
            Assert.Throws<ObjectDisposedException>(() => firstWorld.Create());
            Assert.IsFalse(firstWorld.IsAlive(firstEntity));
            application.Stop();
        }

        /// <summary>
        /// Verifies missing registrations, duplicate registrations, and inactive scene access fail clearly.
        /// </summary>
        [Test]
        public void Configuration_RejectsInvalidEntityUsage()
        {
            var builder = new GameApplicationBuilder()
                .AddApplicationComponent<Position>();
            Assert.Throws<InvalidOperationException>(
                () => builder.AddApplicationComponent<Position>());

            var application = builder.Build();
            application.Start();
            var entities = application.Services.GetRequired<IEntityService>();
            Assert.Throws<InvalidOperationException>(() =>
            {
                var unused = entities.Scene;
            });
            var entity = entities.Application.Create();
            Assert.Throws<InvalidOperationException>(
                () => entities.Application.Add(entity, new Health { Value = 10 }));
            application.Stop();
        }

        /// <summary>
        /// Creates one started application with basic application components.
        /// </summary>
        private static GameApplication CreateApplication(int initialCapacity = 4)
        {
            var application = new GameApplicationBuilder()
                .UseEntities(initialCapacity)
                .AddApplicationComponent<Position>()
                .AddApplicationComponent<Velocity>()
                .Build();
            application.Start();
            return application;
        }

        private struct Position : IEntityComponent
        {
            internal float X;
            internal float Y;
        }

        private struct Velocity : IEntityComponent
        {
            internal float X;
        }

        private struct Health : IEntityComponent
        {
            internal int Value;
        }

        private class RecordingSystem : IEntitySystem
        {
            private readonly List<string> mEvents;

            internal RecordingSystem(List<string> events, int order)
            {
                mEvents = events;
                Order   = order;
            }

            public int Order { get; }

            public virtual void Initialize(EntityWorld world)
            {
                mEvents.Add("Record.Initialize");
            }

            public virtual void Update(in FrameTime time)
            {
                mEvents.Add("Record.Update");
            }

            public virtual void Shutdown()
            {
                mEvents.Add("Record.Shutdown");
            }
        }

        private sealed class MovementSystem : RecordingSystem
        {
            private readonly List<string> mEvents;
            private EntityWorld mWorld;

            internal MovementSystem(List<string> events, int order)
                : base(events, order)
            {
                mEvents = events;
            }

            public override void Initialize(EntityWorld world)
            {
                mWorld = world;
                mEvents.Add("Move.Initialize");
            }

            public override void Update(in FrameTime time)
            {
                var query = mWorld.Query<Position, Velocity>();
                for (int i = 0, cnt = query.CandidateCount; i < cnt; i++)
                {
                    if (!query.TryGetEntity(i, out var entity))
                        continue;
                    ref var position = ref query.GetFirst(entity);
                    ref var velocity = ref query.GetSecond(entity);
                    position.X += velocity.X * (float)time.DeltaTime;
                }
                mEvents.Add("Move.Update");
            }

            public override void Shutdown()
            {
                mEvents.Add("Move.Shutdown");
                mWorld = null;
            }
        }

        private abstract class SceneModuleBase : ModuleBase
        {
            internal EntityWorld ApplicationWorld { get; private set; }
            internal EntityWorld World { get; private set; }
            internal EntityId Entity { get; private set; }

            protected override void OnConfigure(IServiceRegistry services)
            {
                ApplicationWorld = ApplicationEntities;
                World            = SceneEntities;
                Entity           = World.Create();
                World.Add(Entity, new Position { X = 1.0f });
            }
        }

        private sealed class FirstSceneModule : SceneModuleBase { }

        private sealed class SecondSceneModule : SceneModuleBase { }
    }
}
