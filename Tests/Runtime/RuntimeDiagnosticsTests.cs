using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Diagnostics;
using Tritone.Entities;
using Tritone.Flows;

namespace Tritone.Kernel.Tests
{
    /// <summary>
    /// Verifies fixed log retention and sampled runtime diagnostic state.
    /// </summary>
    public sealed class RuntimeDiagnosticsTests
    {
        /// <summary>
        /// Verifies the log buffer overwrites the oldest events at fixed capacity.
        /// </summary>
        [Test]
        public void LogBuffer_RetainsNewestEventsInChronologicalOrder()
        {
            var logs = new RuntimeLogBufferSink(2);
            Write(logs, "First");
            Write(logs, "Second");
            Write(logs, "Third");

            Assert.AreEqual(2, logs.Count);
            Assert.AreEqual("Second", logs.GetAt(0).Message);
            Assert.AreEqual("Third", logs.GetAt(1).Message);
            logs.Clear();
            Assert.AreEqual(0, logs.Count);
        }

        /// <summary>
        /// Verifies diagnostics sample application, module, flow, entities, and frame timing.
        /// </summary>
        [Test]
        public void Update_CapturesRuntimeSnapshot()
        {
            var application = new GameApplicationBuilder()
                .UseRuntimeDiagnostics(sampleWindow: 0.5)
                .AddApplicationComponent<Marker>()
                .AddSceneComponent<Marker>()
                .AddFlow<TestFlow>()
                .AddSceneModule<TestSceneModule>()
                .Build();
            application.Start();
            var entities = application.Services.GetRequired<IEntityService>();
            entities.Application.Create();
            application.SwitchModule<TestSceneModule>();
            application.Services.GetRequired<IFlowService>().SwitchAsync<TestFlow>().GetAwaiter().GetResult();

            Update(application, 0.25, 1);
            Update(application, 0.25, 2);
            var snapshot = application.Services.GetRequired<IRuntimeDiagnosticsService>().Snapshot;

            Assert.AreEqual(EApplicationState.Running, snapshot.ApplicationState);
            Assert.AreEqual(nameof(TestSceneModule), snapshot.ActiveModule);
            Assert.AreEqual(nameof(TestFlow), snapshot.ActiveFlow);
            Assert.AreEqual(1, snapshot.ApplicationEntities);
            Assert.AreEqual(1, snapshot.SceneEntities);
            Assert.AreEqual(4.0, snapshot.FramesPerSecond, 0.0001);
            Assert.AreEqual(250.0, snapshot.AverageFrameMilliseconds, 0.0001);
            Assert.AreEqual((ulong)2, snapshot.FrameIndex);
            application.Stop();
        }

        /// <summary>
        /// Verifies module logs are captured by runtime diagnostics.
        /// </summary>
        [Test]
        public void Logging_CapturesModuleEvents()
        {
            var application = new GameApplicationBuilder()
                .UseRuntimeDiagnostics()
                .AddModule(new LoggingModule())
                .Build();
            application.Start();
            var logs = application.Services.GetRequired<IRuntimeDiagnosticsService>().Logs;

            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual(nameof(LoggingModule), logs.GetAt(0).Category);
            Assert.AreEqual("Started", logs.GetAt(0).Message);
            application.Stop();
        }

        /// <summary>
        /// Writes one deterministic test event.
        /// </summary>
        /// <param name="logs">The destination log buffer.</param>
        /// <param name="message">The event message.</param>
        private static void Write(RuntimeLogBufferSink logs, string message)
        {
            var logEvent = new LogEvent(DateTime.UtcNow, ELogLevel.Info, "Test", message, null);
            logs.Write(in logEvent);
        }

        /// <summary>
        /// Advances one diagnostic sampling frame.
        /// </summary>
        /// <param name="application">The running application.</param>
        /// <param name="deltaTime">The unscaled frame duration.</param>
        /// <param name="frameIndex">The frame index.</param>
        private static void Update(GameApplication application, double deltaTime, ulong frameIndex)
        {
            var time = new FrameTime(deltaTime, deltaTime, deltaTime, frameIndex);
            application.Update(in time);
        }

        /// <summary>
        /// Stores test entity data.
        /// </summary>
        private struct Marker : IEntityComponent { }

        /// <summary>
        /// Represents one test flow.
        /// </summary>
        private sealed class TestFlow : IFlow
        {
            /// <inheritdoc />
            public Task EnterAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public void Update(in FrameTime time) { }

            /// <inheritdoc />
            public void Exit() { }

            /// <inheritdoc />
            public void Dispose() { }
        }

        /// <summary>
        /// Creates one scene entity during configuration.
        /// </summary>
        private sealed class TestSceneModule : ModuleBase
        {
            /// <inheritdoc />
            protected override void OnConfigure(IServiceRegistry services)
            {
                SceneEntities.Create();
            }
        }

        /// <summary>
        /// Writes one startup log event.
        /// </summary>
        private sealed class LoggingModule : ModuleBase
        {
            /// <inheritdoc />
            protected override void OnStart()
            {
                Logger.Info("Started");
            }
        }
    }
}
