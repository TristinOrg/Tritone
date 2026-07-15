using System;
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
    }
}
