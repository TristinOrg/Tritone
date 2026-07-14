using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tritone.Diagnostics;

namespace Tritone.Kernel.Tests
{
    /// <summary>
    /// Verifies diagnostic filtering, dispatch, and resource ownership.
    /// </summary>
    public sealed class LoggerTests
    {
        /// <summary>
        /// Verifies that events below the configured minimum severity are ignored.
        /// </summary>
        [Test]
        public void Log_FiltersEventsBelowMinimumLevel()
        {
            var sink   = new RecordingLogSink();
            var logger = new Logger(ELogLevel.Warning, sink);

            logger.Info("Test", "Ignored");
            logger.Warning("Test", "Accepted");

            Assert.AreEqual(1, sink.Events.Count);
            Assert.AreEqual(ELogLevel.Warning, sink.Events[0].Level);
            Assert.AreEqual("Accepted", sink.Events[0].Message);
        }

        /// <summary>
        /// Verifies that event metadata and exceptions reach every configured sink.
        /// </summary>
        [Test]
        public void Log_DispatchesCompleteEventData()
        {
            var firstSink  = new RecordingLogSink();
            var secondSink = new RecordingLogSink();
            var exception  = new InvalidOperationException("Failure");
            var logger     = new Logger(ELogLevel.Trace, firstSink, secondSink);

            logger.Error("Network", "Connection failed.", exception);

            Assert.AreEqual(1, firstSink.Events.Count);
            Assert.AreEqual(1, secondSink.Events.Count);
            Assert.AreEqual("Network", firstSink.Events[0].Category);
            Assert.AreSame(exception, firstSink.Events[0].Exception);
        }

        /// <summary>
        /// Verifies that application logging infrastructure is disposed after modules stop.
        /// </summary>
        [Test]
        public void UseLogging_DisposesOwnedSinksAfterModulesStop()
        {
            var sink        = new RecordingLogSink();
            var application = new GameApplicationBuilder()
                .UseLogging(ELogLevel.Info, sink)
                .AddModule(new InfoModule())
                .Build();

            application.Start();
            application.Stop();

            Assert.IsTrue(sink.Disposed);
            Assert.AreEqual(2, sink.Events.Count);
        }

        /// <summary>
        /// Verifies automatic module categories and module-specific severity filtering.
        /// </summary>
        [Test]
        public void ModuleLogger_UsesModuleCategoryAndLevel()
        {
            var sink   = new RecordingLogSink();
            var module = new WarningModule();
            var application = new GameApplicationBuilder()
                .UseLogging(ELogLevel.Trace, sink)
                .AddModule(module)
                .Build();

            application.Start();
            application.Stop();

            Assert.AreEqual(1, sink.Events.Count);
            Assert.AreEqual(nameof(WarningModule), sink.Events[0].Category);
            Assert.AreEqual(ELogLevel.Warning, sink.Events[0].Level);
            Assert.AreEqual("Accepted", sink.Events[0].Message);
        }

        /// <summary>
        /// Verifies that ModuleBase remains safe when logging is not configured.
        /// </summary>
        [Test]
        public void ModuleBase_UsesNullLoggerWhenLoggingIsNotConfigured()
        {
            var application = new GameApplicationBuilder()
                .AddModule(new SilentModule())
                .Build();

            Assert.DoesNotThrow(() => application.Start());
            Assert.DoesNotThrow(() => application.Stop());
        }

        /// <summary>
        /// Records diagnostic events for assertions and tracks disposal.
        /// </summary>
        private sealed class RecordingLogSink : ILogSink, IDisposable
        {
            /// <summary>
            /// Stores received diagnostic events in dispatch order.
            /// </summary>
            internal readonly List<LogEvent> Events = new List<LogEvent>();

            /// <summary>
            /// Gets whether the sink has been disposed.
            /// </summary>
            internal bool Disposed { get; private set; }

            /// <summary>
            /// Records one diagnostic event.
            /// </summary>
            /// <param name="logEvent">The immutable event to record.</param>
            public void Write(in LogEvent logEvent)
            {
                Events.Add(logEvent);
            }

            /// <summary>
            /// Marks the sink as disposed.
            /// </summary>
            public void Dispose()
            {
                Disposed = true;
            }
        }

        /// <summary>
        /// Provides a module-specific warning threshold for filtering assertions.
        /// </summary>
        private sealed class WarningModule : ModuleBase
        {
            /// <summary>
            /// Gets the minimum severity accepted by this module.
            /// </summary>
            protected override ELogLevel LogLevel => ELogLevel.Warning;

            /// <summary>
            /// Writes one rejected event and one accepted event.
            /// </summary>
            protected override void OnStart()
            {
                Logger.Debug("Ignored");
                Logger.Warning("Accepted");
            }
        }

        /// <summary>
        /// Writes lifecycle events through the logger supplied by ModuleBase.
        /// </summary>
        private sealed class InfoModule : ModuleBase
        {
            /// <summary>
            /// Writes the module startup event.
            /// </summary>
            protected override void OnStart()
            {
                Logger.Info("Started");
            }

            /// <summary>
            /// Writes the module shutdown event.
            /// </summary>
            protected override void OnStop()
            {
                Logger.Info("Stopped");
            }
        }

        /// <summary>
        /// Writes through the no-op logger supplied by the default infrastructure.
        /// </summary>
        private sealed class SilentModule : ModuleBase
        {
            /// <summary>
            /// Verifies that the default logger can be called safely.
            /// </summary>
            protected override void OnStart()
            {
                Logger.Info("This event is intentionally ignored.");
            }
        }
    }
}
