using System;
using Tritone.Kernel;

namespace Tritone.Timing
{
    /// <summary>
    /// Provides delayed and repeating callbacks driven by the normal update stage.
    /// </summary>
    public sealed class TimerModule : ModuleBase, ITimerService, IUpdateSystem
    {
        /// <summary>
        /// Defines the default initial capacity of each timer heap.
        /// </summary>
        private const int DefaultCapacity = 64;

        /// <summary>
        /// Defines the default callback safety limit for one application update.
        /// </summary>
        private const int DefaultMaxCallbacksPerUpdate = 4096;

        /// <summary>
        /// Stores timers advanced by scaled time.
        /// </summary>
        private readonly TimerQueue mScaledTimers;

        /// <summary>
        /// Stores timers advanced by unscaled time.
        /// </summary>
        private readonly TimerQueue mUnscaledTimers;

        /// <summary>
        /// Stores the maximum number of callbacks allowed during one update.
        /// </summary>
        private readonly int mMaxCallbacksPerUpdate;

        /// <summary>
        /// Stores the update execution order of this module.
        /// </summary>
        private readonly int mOrder;

        /// <summary>
        /// Tracks accumulated scaled time in seconds.
        /// </summary>
        private double mScaledTime;

        /// <summary>
        /// Tracks accumulated unscaled time in seconds.
        /// </summary>
        private double mUnscaledTime;

        /// <summary>
        /// Tracks the most recently allocated timer identifier.
        /// </summary>
        private ulong mNextId;

        /// <summary>
        /// Indicates whether this module has permanently stopped.
        /// </summary>
        private bool mStopped;

        /// <summary>
        /// Initializes a timer module with preallocated timer storage.
        /// </summary>
        /// <param name="capacity">The initial capacity of each timer heap.</param>
        /// <param name="maxCallbacksPerUpdate">The callback safety limit for one update.</param>
        /// <param name="order">The execution order within the normal update stage.</param>
        public TimerModule(int capacity              = DefaultCapacity,
                           int maxCallbacksPerUpdate = DefaultMaxCallbacksPerUpdate,
                           int order                 = 0)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (maxCallbacksPerUpdate < 1)
                throw new ArgumentOutOfRangeException(nameof(maxCallbacksPerUpdate));

            mScaledTimers          = new(capacity);
            mUnscaledTimers        = new(capacity);
            mMaxCallbacksPerUpdate = maxCallbacksPerUpdate;
            mOrder                 = order;
        }

        /// <summary>
        /// Gets the execution order within the normal update stage.
        /// </summary>
        public int Order => mOrder;

        /// <summary>
        /// Registers the timer service for dependent modules.
        /// </summary>
        /// <param name="services">The application-scoped service registry.</param>
        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton<ITimerService>(this);
        }

        /// <summary>
        /// Creates an isolated lifetime scope for one module's timers.
        /// </summary>
        /// <returns>A new timer lifetime scope.</returns>
        public ITimerScope CreateScope()
        {
            if (mStopped)
                throw new InvalidOperationException("Timer scopes cannot be created after the timer module has stopped.");

            return new TimerScope(this);
        }

        /// <summary>
        /// Schedules a timer owned by one lifetime scope.
        /// </summary>
        /// <param name="owner">The scope that owns the timer.</param>
        /// <param name="key">The caller-defined timer key.</param>
        /// <param name="delay">The delay before the first callback.</param>
        /// <param name="interval">The repeat interval, or zero for a one-shot timer.</param>
        /// <param name="callback">The callback invoked whenever the timer expires.</param>
        /// <param name="timeMode">The clock used to advance the timer.</param>
        /// <returns>A handle that can query or cancel the timer.</returns>
        internal TimerHandle SetTimer(TimerScope owner,
                                      TimerKey key,
                                      double delay,
                                      double interval,
                                      Action callback,
                                      ETimerTimeMode timeMode)
        {
            ValidateDuration(delay, interval == 0.0, interval == 0.0 ? nameof(delay) : nameof(interval));
            return ScheduleInternal(owner, key, delay, interval, callback, timeMode);
        }

        /// <summary>
        /// Cancels an active timer.
        /// </summary>
        /// <param name="owner">The scope that must own the timer.</param>
        /// <param name="handle">The timer handle to cancel.</param>
        /// <returns>True when an active timer was cancelled; otherwise, false.</returns>
        internal bool CancelTimer(TimerScope owner, TimerHandle handle)
        {
            if (!handle.IsValid)
                return false;

            return mScaledTimers.Remove(handle.Id, owner) || mUnscaledTimers.Remove(handle.Id, owner);
        }

        /// <summary>
        /// Determines whether a timer is currently active.
        /// </summary>
        /// <param name="owner">The scope that must own the timer.</param>
        /// <param name="handle">The timer handle to query.</param>
        /// <returns>True when the timer is active; otherwise, false.</returns>
        internal bool IsTimerActive(TimerScope owner, TimerHandle handle)
        {
            if (!handle.IsValid)
                return false;

            return mScaledTimers.Contains(handle.Id, owner) || mUnscaledTimers.Contains(handle.Id, owner);
        }

        /// <summary>
        /// Cancels every timer owned by one module scope.
        /// </summary>
        /// <param name="owner">The scope whose timers must be removed.</param>
        internal void CancelAllTimers(TimerScope owner)
        {
            mScaledTimers.RemoveOwner(owner);
            mUnscaledTimers.RemoveOwner(owner);
        }

        /// <summary>
        /// Advances both timer clocks and invokes callbacks that are due.
        /// </summary>
        /// <param name="time">The timing data for the current frame.</param>
        public void Update(in FrameTime time)
        {
            mScaledTime   += Math.Max(0.0, time.DeltaTime);
            mUnscaledTime += Math.Max(0.0, time.UnscaledDeltaTime);

            var remainingCallbacks = mMaxCallbacksPerUpdate;
            remainingCallbacks = ProcessQueue(mScaledTimers, mScaledTime, remainingCallbacks);
            if (remainingCallbacks > 0)
                remainingCallbacks = ProcessQueue(mUnscaledTimers, mUnscaledTime, remainingCallbacks);
            if (remainingCallbacks == 0 &&
                (mScaledTimers.HasDue(mScaledTime) || mUnscaledTimers.HasDue(mUnscaledTime)))
                Logger.Warning($"Timer callback limit reached. Limit: {mMaxCallbacksPerUpdate}.");
        }

        /// <summary>
        /// Releases all callback references and resets timer clocks.
        /// </summary>
        protected override void OnStop()
        {
            mStopped = true;
            mScaledTimers.Clear();
            mUnscaledTimers.Clear();
            mScaledTime   = 0.0;
            mUnscaledTime = 0.0;
            mNextId       = 0;
        }

        /// <summary>
        /// Creates one timer entry and inserts it into the selected clock queue.
        /// </summary>
        /// <param name="owner">The scope that owns the timer.</param>
        /// <param name="key">The caller-defined timer key.</param>
        /// <param name="delay">The delay before the first callback.</param>
        /// <param name="interval">The repeat interval, or zero for a one-shot timer.</param>
        /// <param name="callback">The callback invoked when the timer expires.</param>
        /// <param name="timeMode">The clock used to advance the timer.</param>
        /// <returns>The handle assigned to the timer.</returns>
        private TimerHandle ScheduleInternal(TimerScope owner,
                                              TimerKey key,
                                              double delay,
                                              double interval,
                                              Action callback,
                                              ETimerTimeMode timeMode)
        {
            if (mStopped)
                throw new InvalidOperationException("Timers cannot be scheduled after the timer module has stopped.");
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            TimerQueue queue = GetQueue(timeMode, out var now);
            ulong id        = NextId();
            TimerEntry entry = new()
            {
                Id       = id,
                Key      = key,
                DueTime  = now + delay,
                Interval = interval,
                Callback = callback,
                Owner    = owner
            };
            queue.Add(in entry);
            return new(id);
        }

        /// <summary>
        /// Invokes due callbacks from one queue up to the remaining safety limit.
        /// </summary>
        /// <param name="queue">The timer queue to process.</param>
        /// <param name="now">The current queue clock in seconds.</param>
        /// <param name="remainingCallbacks">The remaining callback allowance for this update.</param>
        /// <returns>The callback allowance remaining after queue processing.</returns>
        private int ProcessQueue(TimerQueue queue, double now, int remainingCallbacks)
        {
            while (remainingCallbacks > 0 && queue.TryPopDue(now, out var entry))
            {
                if (entry.Interval > 0.0)
                {
                    entry.DueTime = now + entry.Interval;
                    queue.Add(in entry);
                }
                else
                    entry.Owner.OnTimerCompleted(entry.Key, entry.Id);

                try
                {
                    entry.Callback.Invoke();
                }
                catch (Exception exception)
                {
                    Logger.Error($"Timer callback failed. Id: {entry.Id}.", exception);
                }
                remainingCallbacks--;
            }

            return remainingCallbacks;
        }

        /// <summary>
        /// Gets the queue and current time associated with a timer time mode.
        /// </summary>
        /// <param name="timeMode">The requested timer clock.</param>
        /// <param name="now">The current time of the selected clock.</param>
        /// <returns>The queue associated with the requested clock.</returns>
        private TimerQueue GetQueue(ETimerTimeMode timeMode, out double now)
        {
            switch (timeMode)
            {
                case ETimerTimeMode.Scaled:
                    now = mScaledTime;
                    return mScaledTimers;
                case ETimerTimeMode.Unscaled:
                    now = mUnscaledTime;
                    return mUnscaledTimers;
                default:
                    throw new ArgumentOutOfRangeException(nameof(timeMode), timeMode, null);
            }
        }

        /// <summary>
        /// Allocates a non-zero identifier not currently owned by either queue.
        /// </summary>
        /// <returns>A unique active timer identifier.</returns>
        private ulong NextId()
        {
            do
            {
                mNextId++;
                if (mNextId == 0)
                    mNextId++;
            }
            while (mScaledTimers.Contains(mNextId) || mUnscaledTimers.Contains(mNextId));

            return mNextId;
        }

        /// <summary>
        /// Validates a timer delay or interval.
        /// </summary>
        /// <param name="duration">The duration to validate.</param>
        /// <param name="allowZero">Whether zero is accepted.</param>
        /// <param name="parameterName">The public parameter name used by validation errors.</param>
        private static void ValidateDuration(double duration, bool allowZero, string parameterName)
        {
            if (double.IsNaN(duration) || double.IsInfinity(duration))
                throw new ArgumentOutOfRangeException(parameterName);
            if ((allowZero && duration < 0.0) || (!allowZero && duration <= 0.0))
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
