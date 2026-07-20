using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Flows;

namespace Tritone.Kernel.Tests
{
    /// <summary>
    /// Verifies flow registration, transitions, rollback, update, and shutdown.
    /// </summary>
    public sealed class FlowTests
    {
        /// <summary>
        /// Verifies lazy flow creation and deterministic application shutdown.
        /// </summary>
        [Test]
        public void SwitchAsync_EntersFlowAndStopReleasesIt()
        {
            List<string> events = new();
            var application     = new GameApplicationBuilder()
                .AddFlow(() => new FirstFlow(events))
                .Build();
            application.Start();
            var flows = application.Services.GetRequired<IFlowService>();
            Assert.IsEmpty(events);

            var flow = flows.SwitchAsync<FirstFlow>().GetAwaiter().GetResult();
            Assert.IsNotNull(flow);
            Assert.AreEqual(typeof(FirstFlow), flows.ActiveFlowType);
            application.Stop();

            CollectionAssert.AreEqual(
                new[] { "First.Enter", "First.Exit", "First.Dispose" },
                events);
        }

        /// <summary>
        /// Verifies active flow update runs before persistent module update.
        /// </summary>
        [Test]
        public void Update_RunsActiveFlowBeforeModules()
        {
            List<string> events = new();
            var application     = new GameApplicationBuilder()
                .AddFlow(() => new FirstFlow(events))
                .AddModule(new RecordingUpdateModule(events))
                .Build();
            application.Start();
            var flows = application.Services.GetRequired<IFlowService>();
            flows.SwitchAsync<FirstFlow>().GetAwaiter().GetResult();
            events.Clear();

            FrameTime time = new(0.016, 0.016, 0.016, 1);
            application.Update(in time);
            application.Stop();

            CollectionAssert.AreEqual(
                new[] { "First.Update", "Module.Update", "First.Exit", "First.Dispose" },
                events);
        }

        /// <summary>
        /// Verifies successful transition releases the previous flow after target entry.
        /// </summary>
        [Test]
        public void SwitchAsync_ReplacesAndReleasesPreviousFlow()
        {
            List<string> events = new();
            var application     = new GameApplicationBuilder()
                .AddFlow(() => new FirstFlow(events))
                .AddFlow(() => new SecondFlow(events))
                .Build();
            application.Start();
            var flows = application.Services.GetRequired<IFlowService>();
            flows.SwitchAsync<FirstFlow>().GetAwaiter().GetResult();

            var second = flows.SwitchAsync<SecondFlow>().GetAwaiter().GetResult();

            Assert.AreEqual(typeof(SecondFlow), flows.ActiveFlowType);
            Assert.IsNotNull(second);
            CollectionAssert.AreEqual(new[]
            {
                "First.Enter",
                "First.Exit",
                "Second.Enter",
                "First.Dispose"
            }, events);
            application.Stop();
        }

        /// <summary>
        /// Verifies failed target entry restores the previous flow instance.
        /// </summary>
        [Test]
        public void SwitchAsync_EntryFailureRollsBackPreviousFlow()
        {
            List<string> events = new();
            var application     = new GameApplicationBuilder()
                .AddFlow(() => new FirstFlow(events))
                .AddFlow(() => new FailingFlow(events))
                .Build();
            application.Start();
            var flows = application.Services.GetRequired<IFlowService>();
            var first = flows.SwitchAsync<FirstFlow>().GetAwaiter().GetResult();

            Assert.Throws<InvalidOperationException>(() =>
                flows.SwitchAsync<FailingFlow>().GetAwaiter().GetResult());

            Assert.AreEqual(typeof(FirstFlow), flows.ActiveFlowType);
            Assert.AreSame(
                first,
                flows.SwitchAsync<FirstFlow>().GetAwaiter().GetResult());
            CollectionAssert.AreEqual(new[]
            {
                "First.Enter",
                "First.Exit",
                "Failing.Enter",
                "Failing.Dispose",
                "First.Enter"
            }, events);
            application.Stop();
        }

        /// <summary>
        /// Verifies identical pending requests share one transition and conflicts are rejected.
        /// </summary>
        [Test]
        public void SwitchAsync_MergesIdenticalRequestAndRejectsConflict()
        {
            List<string> events = new();
            TaskCompletionSource<bool> entry = new();
            var creationCount = 0;
            var application   = new GameApplicationBuilder()
                .AddFlow(() =>
                {
                    creationCount++;
                    return new DelayedFlow(events, entry.Task);
                })
                .AddFlow(() => new SecondFlow(events))
                .Build();
            application.Start();
            var flows = application.Services.GetRequired<IFlowService>();

            var first  = flows.SwitchAsync<DelayedFlow>();
            var second = flows.SwitchAsync<DelayedFlow>();
            Assert.IsTrue(flows.IsSwitching);
            Assert.AreEqual(1, creationCount);
            Assert.Throws<InvalidOperationException>(
                () => flows.SwitchAsync<SecondFlow>().GetAwaiter().GetResult());

            entry.SetResult(true);
            Assert.AreSame(first.GetAwaiter().GetResult(),
                           second.GetAwaiter().GetResult());
            Assert.IsFalse(flows.IsSwitching);
            application.Stop();
        }

        /// <summary>
        /// Verifies cancellation disposes the target and restores the previous flow.
        /// </summary>
        [Test]
        public void SwitchAsync_CancellationRollsBackPreviousFlow()
        {
            List<string> events = new();
            TaskCompletionSource<bool> entry = new();
            var application = new GameApplicationBuilder()
                .AddFlow(() => new FirstFlow(events))
                .AddFlow(() => new DelayedFlow(events, entry.Task))
                .Build();
            application.Start();
            var flows = application.Services.GetRequired<IFlowService>();
            flows.SwitchAsync<FirstFlow>().GetAwaiter().GetResult();
            using CancellationTokenSource cancellation = new();

            var transition = flows.SwitchAsync<DelayedFlow>(cancellation.Token);
            cancellation.Cancel();
            Assert.Throws<TaskCanceledException>(
                () => transition.GetAwaiter().GetResult());

            Assert.AreEqual(typeof(FirstFlow), flows.ActiveFlowType);
            CollectionAssert.AreEqual(new[]
            {
                "First.Enter",
                "First.Exit",
                "Delayed.Enter",
                "Delayed.Dispose",
                "First.Enter"
            }, events);
            application.Stop();
        }

        /// <summary>
        /// Verifies duplicate types, missing registrations, and invalid factories are rejected.
        /// </summary>
        [Test]
        public void Registration_RejectsInvalidConfigurationAndResults()
        {
            var builder = new GameApplicationBuilder().AddFlow<EmptyFlow>();
            Assert.Throws<InvalidOperationException>(
                () => builder.AddFlow<EmptyFlow>());

            var application = new GameApplicationBuilder()
                .AddFlow<EmptyFlow>(() => null)
                .Build();
            application.Start();
            var flows = application.Services.GetRequired<IFlowService>();
            Assert.Throws<InvalidOperationException>(
                () => flows.SwitchAsync<FirstFlow>().GetAwaiter().GetResult());
            Assert.Throws<InvalidOperationException>(() =>
                flows.SwitchAsync<EmptyFlow>().GetAwaiter().GetResult());
            application.Stop();
        }

        /// <summary>
        /// Records one flow lifecycle.
        /// </summary>
        private abstract class RecordingFlow : IFlow
        {
            // Stores the readable flow name.
            private readonly string mName;

            // Stores shared lifecycle output.
            protected readonly List<string> mEvents;

            /// <summary>
            /// Initializes one recording flow.
            /// </summary>
            /// <param name="name">The readable flow name.</param>
            /// <param name="events">The shared lifecycle output.</param>
            protected RecordingFlow(string name, List<string> events)
            {
                mName   = name;
                mEvents = events;
            }

            /// <inheritdoc />
            public virtual Task EnterAsync(CancellationToken cancellationToken)
            {
                mEvents.Add($"{mName}.Enter");
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public void Update(in FrameTime time)
            {
                mEvents.Add($"{mName}.Update");
            }

            /// <inheritdoc />
            public void Exit()
            {
                mEvents.Add($"{mName}.Exit");
            }

            /// <inheritdoc />
            public void Dispose()
            {
                mEvents.Add($"{mName}.Dispose");
            }
        }

        /// <summary>
        /// Represents the first successful flow.
        /// </summary>
        private sealed class FirstFlow : RecordingFlow
        {
            /// <summary>
            /// Initializes the first flow.
            /// </summary>
            /// <param name="events">The shared lifecycle output.</param>
            internal FirstFlow(List<string> events) : base("First", events) { }
        }

        /// <summary>
        /// Represents the second successful flow.
        /// </summary>
        private sealed class SecondFlow : RecordingFlow
        {
            /// <summary>
            /// Initializes the second flow.
            /// </summary>
            /// <param name="events">The shared lifecycle output.</param>
            internal SecondFlow(List<string> events) : base("Second", events) { }
        }

        /// <summary>
        /// Represents a flow that fails during entry.
        /// </summary>
        private sealed class FailingFlow : RecordingFlow
        {
            /// <summary>
            /// Initializes the failing flow.
            /// </summary>
            /// <param name="events">The shared lifecycle output.</param>
            internal FailingFlow(List<string> events) : base("Failing", events) { }

            /// <inheritdoc />
            public override Task EnterAsync(CancellationToken cancellationToken)
            {
                base.EnterAsync(cancellationToken);
                throw new InvalidOperationException("Expected flow entry failure.");
            }
        }

        /// <summary>
        /// Represents a flow that waits for explicit entry completion.
        /// </summary>
        private sealed class DelayedFlow : RecordingFlow
        {
            // Stores the external entry completion.
            private readonly Task mEntry;

            /// <summary>
            /// Initializes one delayed flow.
            /// </summary>
            /// <param name="events">The shared lifecycle output.</param>
            /// <param name="entry">The task that controls entry completion.</param>
            internal DelayedFlow(List<string> events, Task entry)
                : base("Delayed", events)
            {
                mEntry = entry;
            }

            /// <inheritdoc />
            public override Task EnterAsync(
                CancellationToken cancellationToken)
            {
                mEvents.Add("Delayed.Enter");
                Task cancellation = Task.Delay(
                    Timeout.Infinite,
                    cancellationToken);
                return Task.WhenAny(mEntry, cancellation).Unwrap();
            }
        }

        /// <summary>
        /// Represents an empty parameterless flow.
        /// </summary>
        private sealed class EmptyFlow : IFlow
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
        /// Records persistent module update ordering.
        /// </summary>
        private sealed class RecordingUpdateModule : ModuleBase, IUpdateSystem
        {
            // Stores shared update output.
            private readonly List<string> mEvents;

            /// <summary>
            /// Initializes the recording update module.
            /// </summary>
            /// <param name="events">The shared update output.</param>
            internal RecordingUpdateModule(List<string> events)
            {
                mEvents = events;
            }

            /// <inheritdoc />
            public int Order => 0;

            /// <inheritdoc />
            public void Update(in FrameTime time)
            {
                mEvents.Add("Module.Update");
            }
        }
    }
}
