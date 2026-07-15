using System;
using System.Collections.Generic;

namespace Tritone.Timing
{
    /// <summary>
    /// Owns the keyed timers created by one module and releases them as one lifetime group.
    /// </summary>
    internal sealed class TimerScope : ITimerScope
    {
        private const int DefaultCapacity = 8;

        /// <summary>
        /// Maps caller-defined keys to internal scheduler handles.
        /// </summary>
        private readonly Dictionary<TimerKey, TimerHandle> mTimers = new(DefaultCapacity);

        /// <summary>
        /// Stores the timer module that owns the scheduler data.
        /// </summary>
        private TimerModule mTimerModule;

        /// <summary>
        /// Initializes a timer scope owned by one timer module.
        /// </summary>
        /// <param name="timerModule">The module that executes scheduled callbacks.</param>
        internal TimerScope(TimerModule timerModule)
        {
            mTimerModule = timerModule ?? throw new ArgumentNullException(nameof(timerModule));
        }

        /// <inheritdoc />
        public TimerHandle SetTimer(TimerKey key,
                                    double delay,
                                    Action callback,
                                    ETimerTimeMode timeMode = ETimerTimeMode.Scaled)
        {
            return SetInternal(key, delay, 0.0, callback, timeMode);
        }

        /// <inheritdoc />
        public TimerHandle SetRepeatedTimer(TimerKey key,
                                            double interval,
                                            Action callback,
                                            ETimerTimeMode timeMode = ETimerTimeMode.Scaled)
        {
            return SetInternal(key, interval, interval, callback, timeMode);
        }

        /// <inheritdoc />
        public bool CancelTimer(TimerKey key)
        {
            if (mTimerModule == null || !mTimers.TryGetValue(key, out var handle))
                return false;

            var cancelled = mTimerModule.CancelTimer(this, handle);
            mTimers.Remove(key);
            return cancelled;
        }

        /// <inheritdoc />
        public bool IsTimerActive(TimerKey key)
        {
            return mTimerModule != null &&
                   mTimers.TryGetValue(key, out var handle) &&
                   mTimerModule.IsTimerActive(this, handle);
        }

        /// <inheritdoc />
        public void CancelAllTimers()
        {
            if (mTimerModule == null || mTimers.Count == 0)
                return;

            mTimerModule.CancelAllTimers(this);
            mTimers.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (mTimerModule == null)
                return;

            CancelAllTimers();
            mTimerModule = null;
        }

        /// <summary>
        /// Removes a completed one-shot timer without removing a newer timer using the same key.
        /// </summary>
        /// <param name="key">The caller-defined timer key.</param>
        /// <param name="id">The completed internal timer identifier.</param>
        internal void OnTimerCompleted(TimerKey key, ulong id)
        {
            if (mTimers.TryGetValue(key, out var handle) && handle.Id == id)
                mTimers.Remove(key);
        }

        /// <summary>
        /// Replaces an existing key and schedules its new timer.
        /// </summary>
        private TimerHandle SetInternal(TimerKey key,
                                        double delay,
                                        double interval,
                                        Action callback,
                                        ETimerTimeMode timeMode)
        {
            ThrowIfDisposed();
            if (!key.IsValid)
                throw new ArgumentException("A timer key must contain an integer or a non-empty string.", nameof(key));

            CancelTimer(key);
            var handle = mTimerModule.SetTimer(this, key, delay, interval, callback, timeMode);
            mTimers.Add(key, handle);
            return handle;
        }

        /// <summary>
        /// Rejects scheduling after this scope has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (mTimerModule == null)
                throw new ObjectDisposedException(nameof(TimerScope));
        }
    }
}
