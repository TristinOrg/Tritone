using System;
using System.Collections.Generic;
using Tritone.Events;
using Tritone.Timing;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides the standard lifecycle and automatic logging used by most modules.
    /// </summary>
    public abstract class ModuleBase : IModule
    {
        /// <summary>
        /// Stores application services while this module is active.
        /// </summary>
        private IServiceRegistry mServices;

        /// <summary>
        /// Owns every timer created through this module's timer helper methods.
        /// </summary>
        private ITimerScope mTimerScope;

        /// <summary>
        /// Stores event bindings that are automatically released when this module stops.
        /// </summary>
        private List<EventBinding> mEventBindings;

        /// <summary>
        /// Gets the minimum severity accepted by this module.
        /// </summary>
        protected virtual ELogLevel LogLevel => ELogLevel.Info;

        /// <summary>
        /// Gets the logger automatically bound to the concrete module type.
        /// </summary>
        protected IModuleLogger Logger { get; private set; } = NullModuleLogger.Instance;

        /// <summary>
        /// Creates the module logger and invokes module-specific configuration.
        /// </summary>
        /// <param name="context">The immutable application infrastructure available to this module.</param>
        public void Configure(ModuleContext context)
        {
            mServices = context.Services;
            Logger = context.LoggerFactory.Create(GetType(), LogLevel);
            try
            {
                OnConfigure(mServices);
            }
            catch
            {
                ReleaseTimerScope();
                UnbindAllEvents();
                mServices = null;
                Logger = NullModuleLogger.Instance;
                throw;
            }
        }

        /// <summary>
        /// Invokes module-specific startup.
        /// </summary>
        public void Start()
        {
            OnStart();
        }

        /// <summary>
        /// Invokes module-specific shutdown and releases the module logger reference.
        /// </summary>
        public void Stop()
        {
            try
            {
                OnStop();
            }
            finally
            {
                ReleaseTimerScope();
                UnbindAllEvents();
                mServices = null;
                Logger = NullModuleLogger.Instance;
            }
        }

        /// <summary>
        /// Schedules a callback owned by this module to run once after a delay.
        /// </summary>
        /// <param name="key">The caller-defined timer key.</param>
        /// <param name="delay">The non-negative delay in seconds.</param>
        /// <param name="callback">The callback invoked when the timer expires.</param>
        /// <param name="timeMode">The clock used to advance the timer.</param>
        /// <returns>A handle that can query or cancel the timer.</returns>
        protected TimerHandle SetTimer(TimerKey key,
                                       double delay,
                                       Action callback,
                                       ETimerTimeMode timeMode = ETimerTimeMode.Scaled)
        {
            return GetTimerScope().SetTimer(key, delay, callback, timeMode);
        }

        /// <summary>
        /// Schedules a callback owned by this module to run repeatedly at a fixed interval.
        /// </summary>
        /// <param name="key">The caller-defined timer key.</param>
        /// <param name="interval">The positive interval in seconds.</param>
        /// <param name="callback">The callback invoked whenever the timer expires.</param>
        /// <param name="timeMode">The clock used to advance the timer.</param>
        /// <returns>A handle that can query or cancel the timer.</returns>
        protected TimerHandle SetRepeatedTimer(TimerKey key,
                                               double interval,
                                               Action callback,
                                               ETimerTimeMode timeMode = ETimerTimeMode.Scaled)
        {
            return GetTimerScope().SetRepeatedTimer(key, interval, callback, timeMode);
        }

        /// <summary>
        /// Cancels one timer owned by this module.
        /// </summary>
        /// <param name="key">The caller-defined timer key to cancel.</param>
        /// <returns>True when an active timer was cancelled; otherwise, false.</returns>
        protected bool CancelTimer(TimerKey key)
        {
            return mTimerScope != null && mTimerScope.CancelTimer(key);
        }

        /// <summary>
        /// Determines whether this module owns an active timer.
        /// </summary>
        /// <param name="key">The caller-defined timer key to query.</param>
        /// <returns>True when the timer is active; otherwise, false.</returns>
        protected bool IsTimerActive(TimerKey key)
        {
            return mTimerScope != null && mTimerScope.IsTimerActive(key);
        }

        /// <summary>
        /// Cancels every active timer currently owned by this module.
        /// </summary>
        protected void CancelAllTimers()
        {
            mTimerScope?.CancelAllTimers();
        }

        /// <summary>
        /// Binds a parameterless event for the lifetime of this module.
        /// </summary>
        protected void BindEvent(Event eventSource, Action listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a one-parameter event for the lifetime of this module.
        /// </summary>
        protected void BindEvent<T1>(Event<T1> eventSource, Action<T1> listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a two-parameter event for the lifetime of this module.
        /// </summary>
        protected void BindEvent<T1, T2>(Event<T1, T2> eventSource, Action<T1, T2> listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a three-parameter event for the lifetime of this module.
        /// </summary>
        protected void BindEvent<T1, T2, T3>(Event<T1, T2, T3> eventSource, Action<T1, T2, T3> listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a four-parameter event for the lifetime of this module.
        /// </summary>
        protected void BindEvent<T1, T2, T3, T4>(Event<T1, T2, T3, T4> eventSource,
                                                  Action<T1, T2, T3, T4> listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Configures services and dependencies required by the concrete module.
        /// </summary>
        /// <param name="services">The application-scoped service registry.</param>
        protected virtual void OnConfigure(IServiceRegistry services) { }

        /// <summary>
        /// Starts the concrete module.
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// Stops the concrete module.
        /// </summary>
        protected virtual void OnStop() { }

        /// <summary>
        /// Gets or lazily creates the timer scope owned by this module.
        /// </summary>
        /// <returns>The timer scope owned by this module.</returns>
        private ITimerScope GetTimerScope()
        {
            if (mTimerScope != null)
                return mTimerScope;
            if (mServices == null)
                throw new InvalidOperationException("Timers can only be created during an active module lifecycle.");
            if (!mServices.TryGet<ITimerService>(out var timerService))
                throw new InvalidOperationException("Timer infrastructure is not configured. Call builder.UseTimers() before adding game modules.");

            mTimerScope = timerService.CreateScope();
            return mTimerScope;
        }

        /// <summary>
        /// Cancels all module timers and releases the timer scope.
        /// </summary>
        private void ReleaseTimerScope()
        {
            if (mTimerScope == null)
                return;

            mTimerScope.Dispose();
            mTimerScope = null;
        }

        /// <summary>
        /// Stores one binding for automatic module lifetime cleanup.
        /// </summary>
        /// <param name="binding">The newly created event binding.</param>
        private void AddEventBinding(EventBinding binding)
        {
            mEventBindings ??= new();
            mEventBindings.Add(binding);
        }

        /// <summary>
        /// Releases every event listener owned by this module.
        /// </summary>
        private void UnbindAllEvents()
        {
            if (mEventBindings == null)
                return;

            for (int i = mEventBindings.Count - 1; i >= 0; i--)
                mEventBindings[i].Dispose();
            mEventBindings.Clear();
        }
    }
}
