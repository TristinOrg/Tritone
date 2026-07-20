using System;
using Tritone.Timing;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides timer operations whose ownership follows one module context.
    /// </summary>
    public sealed class TimerCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific timer scope.
        private ITimerScope mScope;

        /// <summary>
        /// Initializes timer operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal TimerCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Schedules one callback after a delay.
        /// </summary>
        /// <param name="key">The caller-defined timer key.</param>
        /// <param name="delay">The non-negative delay in seconds.</param>
        /// <param name="callback">The callback invoked after the delay.</param>
        /// <param name="timeMode">The clock used to advance the timer.</param>
        /// <returns>The scheduled timer handle.</returns>
        public TimerHandle Set(TimerKey key,
                               double delay,
                               Action callback,
                               ETimerTimeMode timeMode)
        {
            return GetScope().SetTimer(key, delay, callback, timeMode);
        }

        /// <summary>
        /// Schedules one callback at a repeated interval.
        /// </summary>
        /// <param name="key">The caller-defined timer key.</param>
        /// <param name="interval">The positive interval in seconds.</param>
        /// <param name="callback">The callback invoked at each interval.</param>
        /// <param name="timeMode">The clock used to advance the timer.</param>
        /// <returns>The scheduled timer handle.</returns>
        public TimerHandle SetRepeated(TimerKey key,
                                       double interval,
                                       Action callback,
                                       ETimerTimeMode timeMode)
        {
            return GetScope().SetRepeatedTimer(key, interval, callback, timeMode);
        }

        /// <summary>
        /// Cancels one active timer.
        /// </summary>
        /// <param name="key">The caller-defined timer key.</param>
        /// <returns>True when an active timer was cancelled; otherwise, false.</returns>
        public bool Cancel(TimerKey key)
        {
            return mScope != null && mScope.CancelTimer(key);
        }

        /// <summary>
        /// Determines whether one timer is active.
        /// </summary>
        /// <param name="key">The caller-defined timer key.</param>
        /// <returns>True when the timer is active; otherwise, false.</returns>
        public bool IsActive(TimerKey key)
        {
            return mScope != null && mScope.IsTimerActive(key);
        }

        /// <summary>
        /// Cancels every timer owned by this capability.
        /// </summary>
        public void CancelAll()
        {
            mScope?.CancelAllTimers();
        }

        /// <summary>
        /// Gets or creates the timer scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned timer scope.</returns>
        private ITimerScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<ITimerService>(
                "Timer infrastructure is not configured. Call builder.UseTimers() before adding game modules.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }
    }
}
