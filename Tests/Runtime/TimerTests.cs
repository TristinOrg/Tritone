using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tritone.Timing;

namespace Tritone.Kernel.Tests
{
    /// <summary>
    /// Verifies timer scheduling, clock selection, ordering, cancellation, and failure isolation.
    /// </summary>
    public sealed class TimerTests
    {
        /// <summary>
        /// Verifies that a one-shot timer executes once after its scaled delay.
        /// </summary>
        [Test]
        public void Schedule_ExecutesOnceAfterDelay()
        {
            var callbackCount = 0;
            var application   = CreateApplication(out var timers);
            timers.SetTimer(1, 1.0, () => callbackCount++);

            Update(application, 0.5, 0.5);
            Assert.AreEqual(0, callbackCount);
            Update(application, 0.5, 0.5);
            Assert.AreEqual(1, callbackCount);
            Update(application, 1.0, 1.0);
            Assert.AreEqual(1, callbackCount);

            application.Stop();
        }

        /// <summary>
        /// Verifies that unscaled timers ignore the scaled clock.
        /// </summary>
        [Test]
        public void Schedule_UnscaledTimerUsesUnscaledClock()
        {
            var callbackCount = 0;
            var application   = CreateApplication(out var timers);
            timers.SetTimer("Unscaled", 1.0, () => callbackCount++, ETimerTimeMode.Unscaled);

            Update(application, 10.0, 0.5);
            Assert.AreEqual(0, callbackCount);
            Update(application, 0.0, 0.5);
            Assert.AreEqual(1, callbackCount);

            application.Stop();
        }

        /// <summary>
        /// Verifies that a repeating timer can cancel itself from its callback.
        /// </summary>
        [Test]
        public void ScheduleRepeating_CanCancelItselfInsideCallback()
        {
            var callbackCount = 0;
            var application   = CreateApplication(out var timers);
            timers.SetRepeatedTimer("Repeat", 0.25, () =>
            {
                callbackCount++;
                timers.CancelTimer("Repeat");
            });

            Update(application, 0.25, 0.25);
            Update(application, 1.0, 1.0);

            Assert.AreEqual(1, callbackCount);
            Assert.IsFalse(timers.IsTimerActive("Repeat"));
            application.Stop();
        }

        /// <summary>
        /// Verifies deterministic creation order for timers sharing an expiration time.
        /// </summary>
        [Test]
        public void Schedule_EqualDueTimesUseCreationOrder()
        {
            List<int> order  = new();
            var application = CreateApplication(out var timers);
            timers.SetTimer(1, 1.0, () => order.Add(1));
            timers.SetTimer(2, 1.0, () => order.Add(2));
            timers.SetTimer(3, 1.0, () => order.Add(3));

            Update(application, 1.0, 1.0);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, order);
            application.Stop();
        }

        /// <summary>
        /// Verifies that assigning the same key replaces the previous timer.
        /// </summary>
        [Test]
        public void SetTimer_SameKeyReplacesPreviousTimer()
        {
            var result      = 0;
            var application = CreateApplication(out var timers);
            timers.SetTimer("Refresh", 1.0, () => result = 1);
            timers.SetTimer("Refresh", 1.0, () => result = 2);

            Update(application, 1.0, 1.0);

            Assert.AreEqual(2, result);
            Assert.IsFalse(timers.IsTimerActive("Refresh"));
            application.Stop();
        }

        /// <summary>
        /// Verifies that one callback exception does not block other due timers.
        /// </summary>
        [Test]
        public void Update_CallbackExceptionDoesNotStopQueue()
        {
            var callbackCount = 0;
            var application   = CreateApplication(out var timers);
            timers.SetTimer(1, 0.0, () => throw new InvalidOperationException("Expected"));
            timers.SetTimer(2, 0.0, () => callbackCount++);

            Assert.DoesNotThrow(() => Update(application, 0.0, 0.0));
            Assert.AreEqual(1, callbackCount);
            application.Stop();
        }

        /// <summary>
        /// Verifies that the callback safety limit defers excess due timers to the next update.
        /// </summary>
        [Test]
        public void Update_CallbackLimitDefersRemainingTimers()
        {
            var callbackCount = 0;
            TimerModule timerModule         = new(maxCallbacksPerUpdate: 2);
            GameApplicationBuilder builder  = new();
            GameApplication application     = builder.AddModule(timerModule).Build();
            application.Start();
            var timers = application.Services.GetRequired<ITimerService>().CreateScope();
            timers.SetTimer(1, 0.0, () => callbackCount++);
            timers.SetTimer(2, 0.0, () => callbackCount++);
            timers.SetTimer(3, 0.0, () => callbackCount++);

            Update(application, 0.0, 0.0);
            Assert.AreEqual(2, callbackCount);
            Update(application, 0.0, 0.0);
            Assert.AreEqual(3, callbackCount);

            application.Stop();
        }

        /// <summary>
        /// Creates and starts an application containing one timer module.
        /// </summary>
        /// <param name="timers">The timer service registered by the application.</param>
        /// <returns>The running application.</returns>
        private static GameApplication CreateApplication(out ITimerScope timers)
        {
            GameApplicationBuilder builder = new();
            GameApplication application    = builder.AddModule(new TimerModule()).Build();
            application.Start();
            timers = application.Services.GetRequired<ITimerService>().CreateScope();
            return application;
        }

        /// <summary>
        /// Advances one application update with explicit scaled and unscaled deltas.
        /// </summary>
        /// <param name="application">The running application to update.</param>
        /// <param name="deltaTime">The scaled time advanced by the update.</param>
        /// <param name="unscaledDeltaTime">The unscaled time advanced by the update.</param>
        private static void Update(GameApplication application, double deltaTime, double unscaledDeltaTime)
        {
            FrameTime time = new(deltaTime, unscaledDeltaTime, 0.0, 0);
            application.Update(in time);
        }
    }
}
