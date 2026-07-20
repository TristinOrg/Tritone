using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tritone.Timing;
using Tritone.Tweening;

namespace Tritone.Kernel.Tests
{
    /// <summary>
    /// Verifies tween interpolation, clocks, sequences, controls, and ownership cleanup.
    /// </summary>
    public sealed class TweenTests
    {
        /// <summary>
        /// Verifies linear interpolation reaches the exact target and completes once.
        /// </summary>
        [Test]
        public void Play_InterpolatesAndCompletes()
        {
            var value       = 0.0f;
            var completions = 0;
            var application = CreateApplication(out var tweens);
            var handle = tweens.Play(0.0f,
                                     10.0f,
                                     1.0,
                                     next => value = next,
                                     completed: () => completions++);

            Update(application, 0.25, 0.25);
            Assert.AreEqual(2.5f, value, 0.0001f);
            Assert.IsTrue(tweens.IsActive(handle));
            Update(application, 0.75, 0.75);

            Assert.AreEqual(10.0f, value, 0.0001f);
            Assert.AreEqual(1, completions);
            Assert.IsFalse(tweens.IsActive(handle));
            application.Stop();
        }

        /// <summary>
        /// Verifies built-in easing and unscaled time selection.
        /// </summary>
        [Test]
        public void Play_UsesEasingAndSelectedClock()
        {
            var value       = 0.0f;
            var application = CreateApplication(out var tweens);
            tweens.Play(0.0f,
                        1.0f,
                        1.0,
                        next => value = next,
                        ETweenEase.InQuad,
                        ETimerTimeMode.Unscaled);

            Update(application, 10.0, 0.5);
            Assert.AreEqual(0.25f, value, 0.0001f);
            Update(application, 0.0, 0.5);
            Assert.AreEqual(1.0f, value, 0.0001f);
            application.Stop();
        }

        /// <summary>
        /// Verifies pause, resume, cancel, and active state queries.
        /// </summary>
        [Test]
        public void Controls_PauseResumeAndCancelTween()
        {
            var value       = 0.0f;
            var application = CreateApplication(out var tweens);
            var handle      = tweens.Play(0.0f, 1.0f, 1.0, next => value = next);

            Assert.IsTrue(tweens.Pause(handle));
            Assert.IsTrue(tweens.IsPaused(handle));
            Update(application, 0.5, 0.5);
            Assert.AreEqual(0.0f, value);
            Assert.IsTrue(tweens.Resume(handle));
            Update(application, 0.5, 0.5);
            Assert.AreEqual(0.5f, value, 0.0001f);
            Assert.IsTrue(tweens.Cancel(handle));
            Assert.IsFalse(tweens.IsActive(handle));
            Update(application, 1.0, 1.0);
            Assert.AreEqual(0.5f, value, 0.0001f);
            application.Stop();
        }

        /// <summary>
        /// Verifies sequences carry frame time through tween, callback, and delay boundaries.
        /// </summary>
        [Test]
        public void Sequence_ProcessesStepsAndCarriesOvershoot()
        {
            List<string> events = new();
            var firstValue      = 0.0f;
            var secondValue     = 0.0f;
            var application     = CreateApplication(out var tweens);
            TweenSequence sequence = new TweenSequenceBuilder()
                .Append(0.0f, 10.0f, 0.5, value => firstValue = value)
                .AppendCallback(() => events.Add("Callback"))
                .AppendDelay(0.25)
                .Append(10.0f, 20.0f, 0.5, value => secondValue = value)
                .Build();
            var handle = tweens.Play(sequence,
                                     completed: () => events.Add("Complete"));

            Update(application, 1.0, 1.0);
            Assert.AreEqual(10.0f, firstValue, 0.0001f);
            Assert.AreEqual(15.0f, secondValue, 0.0001f);
            CollectionAssert.AreEqual(new[] { "Callback" }, events);
            Update(application, 0.25, 0.25);

            Assert.AreEqual(20.0f, secondValue, 0.0001f);
            CollectionAssert.AreEqual(new[] { "Callback", "Complete" }, events);
            Assert.IsFalse(tweens.IsActive(handle));
            application.Stop();
        }

        /// <summary>
        /// Verifies finite and infinite sequence loops remain controllable.
        /// </summary>
        [Test]
        public void Sequence_SupportsFiniteAndInfiniteLoops()
        {
            var finiteCount   = 0;
            var infiniteCount = 0;
            var application   = CreateApplication(out var tweens);
            var finite = new TweenSequence(
                TweenStep.Call(() => finiteCount++));
            var infinite = new TweenSequence(
                TweenStep.Call(() => infiniteCount++),
                TweenStep.Delay(0.1));

            var finiteHandle   = tweens.Play(finite, 3);
            var infiniteHandle = tweens.Play(infinite, -1);
            Update(application, 0.0, 0.0);
            Assert.AreEqual(3, finiteCount);
            Assert.IsFalse(tweens.IsActive(finiteHandle));
            Update(application, 0.35, 0.35);
            Assert.GreaterOrEqual(infiniteCount, 3);
            Assert.IsTrue(tweens.Cancel(infiniteHandle));
            application.Stop();
        }

        /// <summary>
        /// Verifies callback failures cancel only their tween and do not stop other entries.
        /// </summary>
        [Test]
        public void Update_CallbackFailureIsIsolated()
        {
            var value       = 0.0f;
            var application = CreateApplication(out var tweens);
            var failing = tweens.Play(0.0f,
                                      1.0f,
                                      0.0,
                                      _ => throw new InvalidOperationException("Expected"));
            tweens.Play(0.0f, 2.0f, 0.0, next => value = next);

            Assert.DoesNotThrow(() => Update(application, 0.0, 0.0));
            Assert.IsFalse(tweens.IsActive(failing));
            Assert.AreEqual(2.0f, value);
            application.Stop();
        }

        /// <summary>
        /// Verifies tweens scheduled from completion begin on the next update.
        /// </summary>
        [Test]
        public void Completion_ScheduledTweenStartsNextUpdate()
        {
            var value       = 0.0f;
            var application = CreateApplication(out var tweens);
            tweens.Delay(0.0, () =>
                tweens.Play(0.0f, 1.0f, 0.0, next => value = next));

            Update(application, 0.0, 0.0);
            Assert.AreEqual(0.0f, value);
            Update(application, 0.0, 0.0);
            Assert.AreEqual(1.0f, value);
            application.Stop();
        }

        /// <summary>
        /// Verifies module shutdown cancels every tween owned through its capability.
        /// </summary>
        [Test]
        public void ModuleStop_CancelsOwnedTweens()
        {
            var consumer    = new TweenConsumerModule();
            var application = new GameApplicationBuilder()
                .UseTweens()
                .AddModule(consumer, typeof(TweenModule))
                .Build();
            application.Start();
            Assert.IsTrue(consumer.IsOwnedTweenActive);

            application.Stop();

            Assert.IsFalse(consumer.IsOwnedTweenActive);
        }

        /// <summary>
        /// Creates one application with a tween scope.
        /// </summary>
        private static GameApplication CreateApplication(out ITweenScope tweens)
        {
            var application = new GameApplicationBuilder()
                .UseTweens()
                .Build();
            application.Start();
            tweens = application.Services.GetRequired<ITweenService>().CreateScope();
            return application;
        }

        /// <summary>
        /// Advances one explicit scaled and unscaled application frame.
        /// </summary>
        private static void Update(GameApplication application,
                                   double deltaTime,
                                   double unscaledDeltaTime)
        {
            FrameTime time = new(deltaTime, unscaledDeltaTime, 0.0, 0);
            application.Update(in time);
        }

        /// <summary>
        /// Schedules one long-running module-owned tween.
        /// </summary>
        private sealed class TweenConsumerModule : ModuleBase
        {
            private TweenHandle mHandle;

            internal bool IsOwnedTweenActive { get; private set; }

            protected override void OnStart()
            {
                mHandle = Tween(0.0f, 1.0f, 100.0, OnValue);
                IsOwnedTweenActive = Context.Tweens.IsActive(mHandle);
            }

            protected override void OnStop()
            {
                IsOwnedTweenActive = false;
            }

            private void OnValue(float value) { }
        }
    }
}
