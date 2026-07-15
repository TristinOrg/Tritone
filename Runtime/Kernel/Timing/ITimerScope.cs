using System;

namespace Tritone.Timing
{
    /// <summary>
    /// Owns every timer created by one module and cancels them as one lifetime group.
    /// </summary>
    public interface ITimerScope : IDisposable
    {
        /// <summary>
        /// Schedules a callback to run once after a delay.
        /// </summary>
        /// <param name="key">The caller-defined key unique within this scope.</param>
        /// <param name="delay">The non-negative delay in seconds.</param>
        /// <param name="callback">The callback invoked when the timer expires.</param>
        /// <param name="timeMode">The clock used to advance the timer.</param>
        /// <returns>A handle that can query or cancel the timer.</returns>
        TimerHandle SetTimer(TimerKey key,
                             double delay,
                             Action callback,
                             ETimerTimeMode timeMode = ETimerTimeMode.Scaled);

        /// <summary>
        /// Schedules a callback to run repeatedly at a fixed interval.
        /// </summary>
        /// <param name="key">The caller-defined key unique within this scope.</param>
        /// <param name="interval">The positive interval in seconds.</param>
        /// <param name="callback">The callback invoked whenever the timer expires.</param>
        /// <param name="timeMode">The clock used to advance the timer.</param>
        /// <returns>A handle that can query or cancel the timer.</returns>
        TimerHandle SetRepeatedTimer(TimerKey key,
                                     double interval,
                                     Action callback,
                                     ETimerTimeMode timeMode = ETimerTimeMode.Scaled);

        /// <summary>
        /// Cancels one active timer owned by this scope.
        /// </summary>
        /// <param name="key">The caller-defined timer key to cancel.</param>
        /// <returns>True when an active timer was cancelled; otherwise, false.</returns>
        bool CancelTimer(TimerKey key);

        /// <summary>
        /// Determines whether this scope owns an active timer.
        /// </summary>
        /// <param name="key">The caller-defined timer key to query.</param>
        /// <returns>True when the timer is active in this scope; otherwise, false.</returns>
        bool IsTimerActive(TimerKey key);

        /// <summary>
        /// Cancels every active timer owned by this scope.
        /// </summary>
        void CancelAllTimers();
    }
}
