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
        public void Tick_UsesUpdateOrder()
        {
            var events      = new List<string>();
            var application = new GameApplicationBuilder()
                .AddModule(new LateUpdateModule(events))
                .AddModule(new EarlyUpdateModule(events))
                .Build();

            application.Start();
            var time = new FrameTime(0.016, 0.016, 0.016, 0);
            application.Tick(in time);
            application.Stop();

            CollectionAssert.AreEqual(new[] { "Early", "Late" }, events);
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
        private sealed class LateUpdateModule : UpdateModule
        {
            /// <summary>
            /// Initializes the late update system.
            /// </summary>
            /// <param name="events">The shared event output list.</param>
            internal LateUpdateModule(List<string> events) : base("Late", 100, events) { }
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
