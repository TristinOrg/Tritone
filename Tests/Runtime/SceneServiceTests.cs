using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Kernel;
using Tritone.Scenes;
using Tritone.Unity.Scenes;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies atomic scene loading, request merging, conflicts, and failure recovery.
    /// </summary>
    public sealed class SceneServiceTests
    {
        /// <summary>
        /// Verifies that the target scene loads before the old module stops and old scene unloads.
        /// </summary>
        [Test]
        public void SwitchAsync_LoadsTargetBeforeChangingModule()
        {
            List<string> events = new();
            TestSceneBackend backend = new("Startup", events);
            var application = CreateApplication(backend, events);
            application.SwitchModule<FirstSceneModule>();
            events.Clear();

            var scenes = application.Services.GetRequired<ISceneService>();
            scenes.SwitchAsync<SecondSceneModule>("Battle")
                  .GetAwaiter()
                  .GetResult();

            CollectionAssert.AreEqual(new[]
            {
                "Load.Battle",
                "Active.Battle",
                "First.Stop",
                "Second.Configure",
                "Second.Start",
                "Unload.Startup"
            }, events);
            Assert.AreEqual("Battle", scenes.ActiveSceneName);
            Assert.AreEqual(typeof(SecondSceneModule), application.ActiveModuleType);
            application.Stop();
        }

        /// <summary>
        /// Verifies that a failed scene load leaves the previous scene and module active.
        /// </summary>
        [Test]
        public void SwitchAsync_LoadFailurePreservesPreviousState()
        {
            List<string> events = new();
            TestSceneBackend backend = new("Startup", events)
            {
                FailLoading = true
            };
            var application = CreateApplication(backend, events);
            application.SwitchModule<FirstSceneModule>();
            events.Clear();

            var scenes = application.Services.GetRequired<ISceneService>();
            Assert.Throws<InvalidOperationException>(() =>
                scenes.SwitchAsync<SecondSceneModule>("Broken").GetAwaiter().GetResult());

            CollectionAssert.AreEqual(new[] { "Load.Broken" }, events);
            Assert.AreEqual("Startup", scenes.ActiveSceneName);
            Assert.AreEqual(typeof(FirstSceneModule), application.ActiveModuleType);
            application.Stop();
        }

        /// <summary>
        /// Verifies that identical concurrent requests share one backend load.
        /// </summary>
        [Test]
        public void SwitchAsync_MergesIdenticalConcurrentRequests()
        {
            List<string> events = new();
            TestSceneBackend backend = new("Startup", events)
            {
                DelayLoading = true
            };
            var application = CreateApplication(backend, events);
            var scenes      = application.Services.GetRequired<ISceneService>();

            var first  = scenes.SwitchAsync<SecondSceneModule>("Battle");
            var second = scenes.SwitchAsync<SecondSceneModule>("Battle");

            Assert.IsTrue(scenes.IsSwitching);
            Assert.AreEqual(1, backend.LoadCount);
            backend.CompleteLoading();
            Assert.AreSame(first.GetAwaiter().GetResult(),
                           second.GetAwaiter().GetResult());
            Assert.IsFalse(scenes.IsSwitching);
            application.Stop();
        }

        /// <summary>
        /// Verifies that a different target cannot overlap one active transition.
        /// </summary>
        [Test]
        public void SwitchAsync_RejectsConflictingConcurrentRequest()
        {
            List<string> events = new();
            TestSceneBackend backend = new("Startup", events)
            {
                DelayLoading = true
            };
            var application = CreateApplication(backend, events);
            var scenes      = application.Services.GetRequired<ISceneService>();

            var running = scenes.SwitchAsync<SecondSceneModule>("Battle");
            Assert.Throws<InvalidOperationException>(() =>
                scenes.SwitchAsync<FirstSceneModule>("Login").GetAwaiter().GetResult());

            backend.CompleteLoading();
            running.GetAwaiter().GetResult();
            application.Stop();
        }

        /// <summary>
        /// Verifies that module startup failure restores the previous scene and a fresh module.
        /// </summary>
        [Test]
        public void SwitchAsync_ModuleFailureRestoresPreviousSceneAndModule()
        {
            List<string> events = new();
            TestSceneBackend backend = new("Startup", events);
            var application = CreateApplication(backend, events);
            application.SwitchModule<FirstSceneModule>();
            events.Clear();

            var scenes = application.Services.GetRequired<ISceneService>();
            Assert.Throws<InvalidOperationException>(() =>
                scenes.SwitchAsync<FailingSceneModule>("Broken").GetAwaiter().GetResult());

            Assert.AreEqual("Startup", backend.ActiveSceneName);
            Assert.AreEqual("Startup", scenes.ActiveSceneName);
            Assert.AreEqual(typeof(FirstSceneModule), application.ActiveModuleType);
            Assert.IsFalse(backend.IsLoaded("Broken"));
            application.Stop();
        }

        /// <summary>
        /// Creates one started application with deterministic scene infrastructure.
        /// </summary>
        /// <param name="backend">The deterministic scene backend.</param>
        /// <param name="events">The shared lifecycle event output.</param>
        /// <returns>The started application.</returns>
        private static GameApplication CreateApplication(TestSceneBackend backend,
                                                         List<string> events)
        {
            var application = new GameApplicationBuilder()
                .UseScenes(backend)
                .AddSceneModule(() => new FirstSceneModule(events))
                .AddSceneModule(() => new SecondSceneModule(events))
                .AddSceneModule(() => new FailingSceneModule(events))
                .Build();
            application.Start();
            return application;
        }

        /// <summary>
        /// Records the first scene module lifecycle.
        /// </summary>
        private sealed class FirstSceneModule : RecordingSceneModule
        {
            /// <summary>
            /// Initializes the first scene module.
            /// </summary>
            /// <param name="events">The shared lifecycle event output.</param>
            internal FirstSceneModule(List<string> events) : base("First", events) { }
        }

        /// <summary>
        /// Records the second scene module lifecycle.
        /// </summary>
        private sealed class SecondSceneModule : RecordingSceneModule
        {
            /// <summary>
            /// Initializes the second scene module.
            /// </summary>
            /// <param name="events">The shared lifecycle event output.</param>
            internal SecondSceneModule(List<string> events) : base("Second", events) { }
        }

        /// <summary>
        /// Throws during startup to verify scene transition rollback.
        /// </summary>
        private sealed class FailingSceneModule : RecordingSceneModule
        {
            /// <summary>
            /// Initializes the failing scene module.
            /// </summary>
            /// <param name="events">The shared lifecycle event output.</param>
            internal FailingSceneModule(List<string> events) : base("Failing", events) { }

            /// <inheritdoc />
            public override void Start()
            {
                base.Start();
                throw new InvalidOperationException("Expected scene module failure.");
            }
        }

        /// <summary>
        /// Records scene module lifecycle callbacks.
        /// </summary>
        private abstract class RecordingSceneModule : IModule
        {
            // Stores the readable scene module name.
            private readonly string mName;

            // Stores the shared lifecycle event output.
            private readonly List<string> mEvents;

            /// <summary>
            /// Initializes one recording scene module.
            /// </summary>
            /// <param name="name">The readable module name.</param>
            /// <param name="events">The shared lifecycle event output.</param>
            protected RecordingSceneModule(string name, List<string> events)
            {
                mName   = name;
                mEvents = events;
            }

            /// <inheritdoc />
            public void Configure(ModuleContext context)
            {
                mEvents.Add($"{mName}.Configure");
            }

            /// <inheritdoc />
            public virtual void Start()
            {
                mEvents.Add($"{mName}.Start");
            }

            /// <inheritdoc />
            public void Stop()
            {
                mEvents.Add($"{mName}.Stop");
            }
        }

        /// <summary>
        /// Provides deterministic additive loading and unloading for scene tests.
        /// </summary>
        private sealed class TestSceneBackend : ISceneBackend
        {
            // Stores loaded scene names.
            private readonly HashSet<string> mLoadedScenes = new(StringComparer.Ordinal);

            // Stores the shared operation event output.
            private readonly List<string> mEvents;

            // Stores one delayed loading completion.
            private TaskCompletionSource<bool> mLoadCompletion;

            // Gets whether loading should fail.
            internal bool FailLoading { get; set; }

            // Gets whether loading waits for explicit completion.
            internal bool DelayLoading { get; set; }

            // Gets the number of backend load operations.
            internal int LoadCount { get; private set; }

            /// <inheritdoc />
            public string ActiveSceneName { get; private set; }

            /// <summary>
            /// Initializes one backend with a loaded active scene.
            /// </summary>
            /// <param name="activeSceneName">The initial active scene name.</param>
            /// <param name="events">The shared operation event output.</param>
            internal TestSceneBackend(string activeSceneName, List<string> events)
            {
                ActiveSceneName = activeSceneName;
                mEvents         = events;
                mLoadedScenes.Add(activeSceneName);
            }

            /// <inheritdoc />
            public bool IsLoaded(string sceneName)
            {
                return mLoadedScenes.Contains(sceneName);
            }

            /// <inheritdoc />
            public async Task LoadAsync(string sceneName, Action<float> progress)
            {
                LoadCount++;
                mEvents.Add($"Load.{sceneName}");
                if (FailLoading)
                    throw new InvalidOperationException("Expected scene loading failure.");
                if (DelayLoading)
                {
                    mLoadCompletion = new();
                    await mLoadCompletion.Task;
                }

                mLoadedScenes.Add(sceneName);
                progress?.Invoke(1.0f);
            }

            /// <inheritdoc />
            public void SetActive(string sceneName)
            {
                if (!mLoadedScenes.Contains(sceneName))
                    throw new InvalidOperationException($"Scene '{sceneName}' is not loaded.");

                ActiveSceneName = sceneName;
                mEvents.Add($"Active.{sceneName}");
            }

            /// <inheritdoc />
            public Task UnloadAsync(string sceneName)
            {
                mLoadedScenes.Remove(sceneName);
                mEvents.Add($"Unload.{sceneName}");
                return Task.CompletedTask;
            }

            /// <summary>
            /// Completes one delayed load.
            /// </summary>
            internal void CompleteLoading()
            {
                var completion = mLoadCompletion;
                mLoadCompletion = null;
                completion.SetResult(true);
            }
        }
    }
}
