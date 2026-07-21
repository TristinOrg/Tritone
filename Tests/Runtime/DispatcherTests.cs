using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Dispatching;
using Tritone.Kernel;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies cross-thread posting, cancellation, bounds, and deterministic scope cleanup.
    /// </summary>
    public sealed class DispatcherTests
    {
        /// <summary>
        /// Stores the scope used by the worker-thread test method group.
        /// </summary>
        private IMainThreadDispatchScope mWorkerScope;

        /// <summary>
        /// Stores the number of callbacks observed by test method groups.
        /// </summary>
        private int mCallbackCount;

        /// <summary>
        /// Stores the decimal callback order observed across bounded frames.
        /// </summary>
        private int mOrderValue;

        /// <summary>
        /// Resets mutable fixture state before every dispatcher test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mWorkerScope   = null;
            mCallbackCount = 0;
            mOrderValue    = 0;
        }

        /// <summary>
        /// Verifies that a worker thread post executes only during application pre-update.
        /// </summary>
        [Test]
        public void Post_FromWorkerExecutesDuringPreUpdate()
        {
            var application = new GameApplicationBuilder().UseMainThreadDispatcher().Build();
            application.Start();
            mWorkerScope = application.Services.GetRequired<IMainThreadDispatcherService>().CreateScope();

            Task.Run(PostFromWorker).GetAwaiter().GetResult();
            Assert.AreEqual(0, mCallbackCount);
            FrameTime time = new(0.016, 0.016, 0.016, 0);
            application.Update(in time);

            Assert.AreEqual(1, mCallbackCount);
            mWorkerScope.Dispose();
            mWorkerScope = null;
            application.Stop();
        }

        /// <summary>
        /// Verifies that cancellation and scope disposal suppress queued callbacks.
        /// </summary>
        [Test]
        public void Scope_CancellationAndDisposalSuppressCallbacks()
        {
            var application = new GameApplicationBuilder().UseMainThreadDispatcher().Build();
            application.Start();
            var scope = application.Services.GetRequired<IMainThreadDispatcherService>().CreateScope();
            var cancelled = scope.Post(IncrementCallbackCount);
            scope.Post(IncrementCallbackCount);

            Assert.IsTrue(scope.IsPending(cancelled));
            Assert.IsTrue(scope.Cancel(cancelled));
            scope.Dispose();
            FrameTime time = new(0.016, 0.016, 0.016, 0);
            application.Update(in time);

            Assert.AreEqual(0, mCallbackCount);
            application.Stop();
        }

        /// <summary>
        /// Verifies that the per-frame safety limit preserves remaining callback order.
        /// </summary>
        [Test]
        public void PreUpdate_RespectsCallbackLimitAcrossFrames()
        {
            var application = new GameApplicationBuilder().UseMainThreadDispatcher(2).Build();
            application.Start();
            var scope = application.Services.GetRequired<IMainThreadDispatcherService>().CreateScope();
            scope.Post(AppendOne);
            scope.Post(AppendTwo);
            scope.Post(AppendThree);
            FrameTime time = new(0.016, 0.016, 0.016, 0);

            application.Update(in time);
            Assert.AreEqual(12, mOrderValue);
            application.Update(in time);
            Assert.AreEqual(123, mOrderValue);

            scope.Dispose();
            application.Stop();
        }

        /// <summary>
        /// Posts the shared callback through the active scope from a worker thread.
        /// </summary>
        private void PostFromWorker()
        {
            mWorkerScope.Post(IncrementCallbackCount);
        }

        /// <summary>
        /// Increments the shared callback observation count.
        /// </summary>
        private void IncrementCallbackCount()
        {
            Interlocked.Increment(ref mCallbackCount);
        }

        /// <summary>
        /// Appends one to the observed callback order.
        /// </summary>
        private void AppendOne()
        {
            mOrderValue = mOrderValue * 10 + 1;
        }

        /// <summary>
        /// Appends two to the observed callback order.
        /// </summary>
        private void AppendTwo()
        {
            mOrderValue = mOrderValue * 10 + 2;
        }

        /// <summary>
        /// Appends three to the observed callback order.
        /// </summary>
        private void AppendThree()
        {
            mOrderValue = mOrderValue * 10 + 3;
        }
    }
}
