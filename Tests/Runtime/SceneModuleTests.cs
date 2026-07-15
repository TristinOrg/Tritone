using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Tritone.Kernel.Tests
{
    /// <summary>
    /// Verifies dynamic scene module creation, switching, lookup, and cleanup.
    /// </summary>
    public sealed class SceneModuleTests
    {
        /// <summary>
        /// Verifies that the configured initial scene module starts with the application.
        /// </summary>
        [Test]
        public void Start_ActivatesInitialSceneModule()
        {
            List<string> events = new();
            var application     = new GameApplicationBuilder()
                .AddSceneModule(() => new FirstSceneModule(events, 1))
                .UseInitialSceneModule<FirstSceneModule>()
                .Build();

            application.Start();

            Assert.AreEqual(typeof(FirstSceneModule), application.ActiveModuleType);
            CollectionAssert.AreEqual(new[] { "First1.Configure", "First1.Start" }, events);
            application.Stop();
        }

        /// <summary>
        /// Verifies that switching stops the previous module before starting a fresh module.
        /// </summary>
        [Test]
        public void SwitchModule_StopsPreviousAndCreatesFreshInstance()
        {
            List<string> events = new();
            var creationCount   = 0;
            var application     = new GameApplicationBuilder()
                .AddSceneModule(() => new FirstSceneModule(events, ++creationCount))
                .AddSceneModule(() => new SecondSceneModule(events))
                .Build();
            application.Start();

            var first = application.SwitchModule<FirstSceneModule>();
            application.SwitchModule<SecondSceneModule>();
            var nextFirst = application.SwitchModule<FirstSceneModule>();

            Assert.AreNotSame(first, nextFirst);
            Assert.AreEqual(2, creationCount);
            CollectionAssert.AreEqual(new[]
            {
                "First1.Configure", "First1.Start", "First1.Stop",
                "Second.Configure", "Second.Start", "Second.Stop",
                "First2.Configure", "First2.Start"
            }, events);
            application.Stop();
        }

        /// <summary>
        /// Verifies that only the active scene module is available from application services.
        /// </summary>
        [Test]
        public void SwitchModule_UpdatesActiveServiceLookup()
        {
            List<string> events = new();
            var application     = new GameApplicationBuilder()
                .AddSceneModule(() => new FirstSceneModule(events, 1))
                .AddSceneModule(() => new SecondSceneModule(events))
                .Build();
            application.Start();

            var first = application.SwitchModule<FirstSceneModule>();
            Assert.AreSame(first, application.Services.GetRequired<FirstSceneModule>());
            application.SwitchModule<SecondSceneModule>();

            Assert.Throws<InvalidOperationException>(() => application.Services.GetRequired<FirstSceneModule>());
            Assert.AreEqual(typeof(SecondSceneModule), application.ActiveModuleType);
            application.Stop();
        }

        private abstract class RecordingSceneModule : IModule
        {
            private readonly string       mName;
            private readonly List<string> mEvents;

            protected RecordingSceneModule(string name, List<string> events)
            {
                mName   = name;
                mEvents = events;
            }

            public void Configure(ModuleContext context)
            {
                mEvents.Add($"{mName}.Configure");
            }

            public void Start()
            {
                mEvents.Add($"{mName}.Start");
            }

            public void Stop()
            {
                mEvents.Add($"{mName}.Stop");
            }
        }

        private sealed class FirstSceneModule : RecordingSceneModule
        {
            internal FirstSceneModule(List<string> events, int index) : base($"First{index}", events) { }
        }

        private sealed class SecondSceneModule : RecordingSceneModule
        {
            internal SecondSceneModule(List<string> events) : base("Second", events) { }
        }
    }
}
