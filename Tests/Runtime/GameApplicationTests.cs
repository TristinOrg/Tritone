using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Tritone.Kernel.Tests
{
    /// <summary>
    /// Verifies the fundamental application lifecycle and update guarantees.
    /// </summary>
    public sealed class GameApplicationTests
    {
        /// <summary>
        /// Verifies dependency-ordered startup and reverse-ordered shutdown.
        /// </summary>
        [Test]
        public void StartAndStop_FollowDependencyOrder()
        {
            var events     = new List<string>();
            var foundation = new RecordingModuleDependency(events);
            var gameplay   = new RecordingModule("Gameplay", events);
            var application = new GameApplicationBuilder()
                .AddModule(gameplay, typeof(RecordingModuleDependency))
                .AddModule(foundation)
                .Build();

            application.Start();
            application.Stop();

            CollectionAssert.AreEqual(new[]
            {
                "Foundation.Configure", "Gameplay.Configure",
                "Foundation.Start", "Gameplay.Start",
                "Gameplay.Stop", "Foundation.Stop"
            }, events);
        }

        /// <summary>
        /// Verifies that application construction rejects circular module dependencies.
        /// </summary>
        [Test]
        public void Build_RejectsCircularDependencies()
        {
            var builder = new GameApplicationBuilder()
                .AddModule(new ModuleA(), typeof(ModuleB))
                .AddModule(new ModuleB(), typeof(ModuleA));

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
            StringAssert.Contains("Circular module dependency", exception.Message);
        }

        /// <summary>
        /// Verifies that lower update order values execute first.
        /// </summary>
        [Test]
        public void Update_UsesSystemOrder()
        {
            var events      = new List<string>();
            var application = new GameApplicationBuilder()
                .AddModule(new HighOrderUpdateModule(events))
                .AddModule(new EarlyUpdateModule(events))
                .Build();

            application.Start();
            var time = new FrameTime(0.016, 0.016, 0.016, 0);
            application.Update(in time);
            application.Stop();

            CollectionAssert.AreEqual(new[] { "Early", "Late" }, events);
        }

        /// <summary>
        /// Verifies stage order and ensures stopped applications reject every update stage.
        /// </summary>
        [Test]
        public void UpdateStages_DispatchInExpectedOrderOnlyWhileRunning()
        {
            var events      = new List<string>();
            var application = new GameApplicationBuilder()
                .AddModule(new AllStageModule(events))
                .Build();
            var time = new FrameTime(0.016, 0.016, 0.016, 0);

            application.Start();
            application.Update(in time);
            application.LateUpdate(in time);
            application.FixedUpdate(in time);
            application.Stop();
            application.Update(in time);
            application.LateUpdate(in time);
            application.FixedUpdate(in time);

            CollectionAssert.AreEqual(new[] { "PreUpdate", "Update", "LateUpdate", "FixedUpdate" }, events);
        }

        /// <summary>
        /// Records lifecycle callbacks for order assertions.
        /// </summary>
        private class RecordingModule : IModule
        {
            /// <summary>
            /// Stores the readable module name used in recorded events.
            /// </summary>
            private readonly string mName;

            /// <summary>
            /// Stores lifecycle events in execution order.
            /// </summary>
            private readonly List<string> mEvents;

            /// <summary>
            /// Initializes a recording module.
            /// </summary>
            /// <param name="name">The readable module name.</param>
            /// <param name="events">The shared event output list.</param>
            internal RecordingModule(string name, List<string> events)
            {
                mName   = name;
                mEvents = events;
            }

            /// <summary>
            /// Records module configuration.
            /// </summary>
            /// <param name="context">The immutable application infrastructure.</param>
            public void Configure(ModuleContext context)
            {
                mEvents.Add($"{mName}.Configure");
            }

            /// <summary>
            /// Records module startup.
            /// </summary>
            public void Start()
            {
                mEvents.Add($"{mName}.Start");
            }

            /// <summary>
            /// Records module shutdown.
            /// </summary>
            public void Stop()
            {
                mEvents.Add($"{mName}.Stop");
            }
        }

        /// <summary>
        /// Represents the foundation dependency used by the lifecycle test.
        /// </summary>
        private sealed class RecordingModuleDependency : RecordingModule
        {
            /// <summary>
            /// Initializes the foundation dependency module.
            /// </summary>
            /// <param name="events">The shared event output list.</param>
            internal RecordingModuleDependency(List<string> events) : base("Foundation", events) { }
        }

        /// <summary>
        /// Records one update callback for update order assertions.
        /// </summary>
        private class UpdateModule : IModule, IUpdateSystem
        {
            /// <summary>
            /// Stores the readable update system name.
            /// </summary>
            private readonly string mName;

            /// <summary>
            /// Stores update events in execution order.
            /// </summary>
            private readonly List<string> mEvents;

            /// <summary>
            /// Initializes an update module.
            /// </summary>
            /// <param name="name">The readable update system name.</param>
            /// <param name="order">The update execution order.</param>
            /// <param name="events">The shared event output list.</param>
            internal UpdateModule(string name, int order, List<string> events)
            {
                mName   = name;
                Order   = order;
                mEvents = events;
            }

            /// <summary>
            /// Gets the update execution order.
            /// </summary>
            public int Order { get; }

            /// <summary>
            /// Performs no configuration for this test module.
            /// </summary>
            /// <param name="context">The immutable application infrastructure.</param>
            public void Configure(ModuleContext context) { }

            /// <summary>
            /// Performs no startup work for this test module.
            /// </summary>
            public void Start() { }

            /// <summary>
            /// Performs no shutdown work for this test module.
            /// </summary>
            public void Stop() { }

            /// <summary>
            /// Records one update callback.
            /// </summary>
            /// <param name="time">The timing data for the current frame.</param>
            public void Update(in FrameTime time)
            {
                mEvents.Add(mName);
            }
        }

        /// <summary>
        /// Represents the early update system.
        /// </summary>
        private sealed class EarlyUpdateModule : UpdateModule
        {
            /// <summary>
            /// Initializes the early update system.
            /// </summary>
            /// <param name="events">The shared event output list.</param>
            internal EarlyUpdateModule(List<string> events) : base("Early", -100, events) { }
        }

        /// <summary>
        /// Represents the late update system.
        /// </summary>
        private sealed class HighOrderUpdateModule : UpdateModule
        {
            /// <summary>
            /// Initializes the late update system.
            /// </summary>
            /// <param name="events">The shared event output list.</param>
            internal HighOrderUpdateModule(List<string> events) : base("Late", 100, events) { }
        }

        /// <summary>
        /// Records callbacks from every application update stage.
        /// </summary>
        private sealed class AllStageModule : ModuleBase,
                                              IPreUpdateSystem,
                                              IUpdateSystem,
                                              ILateUpdateSystem,
                                              IFixedUpdateSystem
        {
            /// <summary>
            /// Stores update stage events in execution order.
            /// </summary>
            private readonly List<string> mEvents;

            /// <summary>
            /// Initializes a module that participates in every update stage.
            /// </summary>
            /// <param name="events">The shared event output list.</param>
            internal AllStageModule(List<string> events)
            {
                mEvents = events;
            }

            /// <summary>
            /// Gets the execution order used in every update stage.
            /// </summary>
            public int Order => 0;

            /// <summary>
            /// Records the pre-update callback.
            /// </summary>
            /// <param name="time">The timing data for the current frame.</param>
            public void PreUpdate(in FrameTime time)
            {
                mEvents.Add("PreUpdate");
            }

            /// <summary>
            /// Records the normal update callback.
            /// </summary>
            /// <param name="time">The timing data for the current frame.</param>
            public void Update(in FrameTime time)
            {
                mEvents.Add("Update");
            }

            /// <summary>
            /// Records the late-update callback.
            /// </summary>
            /// <param name="time">The timing data for the current frame.</param>
            public void LateUpdate(in FrameTime time)
            {
                mEvents.Add("LateUpdate");
            }

            /// <summary>
            /// Records the fixed-update callback.
            /// </summary>
            /// <param name="time">The timing data for the current fixed update.</param>
            public void FixedUpdate(in FrameTime time)
            {
                mEvents.Add("FixedUpdate");
            }
        }

        /// <summary>
        /// Represents the first half of a circular dependency.
        /// </summary>
        private sealed class ModuleA : IModule
        {
            /// <summary>
            /// Performs no configuration for this test module.
            /// </summary>
            /// <param name="context">The immutable application infrastructure.</param>
            public void Configure(ModuleContext context) { }

            /// <summary>
            /// Performs no startup work for this test module.
            /// </summary>
            public void Start() { }

            /// <summary>
            /// Performs no shutdown work for this test module.
            /// </summary>
            public void Stop() { }
        }

        /// <summary>
        /// Represents the second half of a circular dependency.
        /// </summary>
        private sealed class ModuleB : IModule
        {
            /// <summary>
            /// Performs no configuration for this test module.
            /// </summary>
            /// <param name="context">The immutable application infrastructure.</param>
            public void Configure(ModuleContext context) { }

            /// <summary>
            /// Performs no startup work for this test module.
            /// </summary>
            public void Start() { }

            /// <summary>
            /// Performs no shutdown work for this test module.
            /// </summary>
            public void Stop() { }
        }
    }
}
