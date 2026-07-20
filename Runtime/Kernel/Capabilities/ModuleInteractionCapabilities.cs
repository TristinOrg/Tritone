using System;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Events;
using Tritone.Input;
using Tritone.Networking;
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

    /// <summary>
    /// Provides event bindings whose ownership follows one module context.
    /// </summary>
    public sealed class EventCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        /// <summary>
        /// Initializes event operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal EventCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Binds one parameterless event for the module lifetime.
        /// </summary>
        /// <param name="eventSource">The event that owns the listener.</param>
        /// <param name="listener">The listener invoked when the event is published.</param>
        public void Bind(Event eventSource, Action listener)
        {
            mContext.Scope.Own(
                eventSource?.Bind(listener) ??
                throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds one single-value event for the module lifetime.
        /// </summary>
        /// <typeparam name="T1">The published value type.</typeparam>
        /// <param name="eventSource">The event that owns the listener.</param>
        /// <param name="listener">The listener invoked when the event is published.</param>
        public void Bind<T1>(Event<T1> eventSource, Action<T1> listener)
        {
            mContext.Scope.Own(
                eventSource?.Bind(listener) ??
                throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds one two-value event for the module lifetime.
        /// </summary>
        /// <typeparam name="T1">The first published value type.</typeparam>
        /// <typeparam name="T2">The second published value type.</typeparam>
        /// <param name="eventSource">The event that owns the listener.</param>
        /// <param name="listener">The listener invoked when the event is published.</param>
        public void Bind<T1, T2>(Event<T1, T2> eventSource, Action<T1, T2> listener)
        {
            mContext.Scope.Own(
                eventSource?.Bind(listener) ??
                throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds one three-value event for the module lifetime.
        /// </summary>
        /// <typeparam name="T1">The first published value type.</typeparam>
        /// <typeparam name="T2">The second published value type.</typeparam>
        /// <typeparam name="T3">The third published value type.</typeparam>
        /// <param name="eventSource">The event that owns the listener.</param>
        /// <param name="listener">The listener invoked when the event is published.</param>
        public void Bind<T1, T2, T3>(Event<T1, T2, T3> eventSource,
                                     Action<T1, T2, T3> listener)
        {
            mContext.Scope.Own(
                eventSource?.Bind(listener) ??
                throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds one four-value event for the module lifetime.
        /// </summary>
        /// <typeparam name="T1">The first published value type.</typeparam>
        /// <typeparam name="T2">The second published value type.</typeparam>
        /// <typeparam name="T3">The third published value type.</typeparam>
        /// <typeparam name="T4">The fourth published value type.</typeparam>
        /// <param name="eventSource">The event that owns the listener.</param>
        /// <param name="listener">The listener invoked when the event is published.</param>
        public void Bind<T1, T2, T3, T4>(Event<T1, T2, T3, T4> eventSource,
                                         Action<T1, T2, T3, T4> listener)
        {
            mContext.Scope.Own(
                eventSource?.Bind(listener) ??
                throw new ArgumentNullException(nameof(eventSource)));
        }
    }

    /// <summary>
    /// Provides input bindings whose ownership follows one module context.
    /// </summary>
    public sealed class InputCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific input scope.
        private IInputScope mScope;

        /// <summary>
        /// Initializes input operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal InputCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Binds one named button-down callback.
        /// </summary>
        /// <param name="action">The configured input action name.</param>
        /// <param name="callback">The callback invoked on button down.</param>
        public void Bind(string action, Action callback)
        {
            GetScope().BindButton(action, callback);
        }

        /// <summary>
        /// Binds one named axis callback.
        /// </summary>
        /// <param name="action">The configured input action name.</param>
        /// <param name="callback">The callback invoked after a meaningful value change.</param>
        /// <param name="deadZone">The minimum meaningful axis magnitude and change.</param>
        public void BindAxis(string action, Action<float> callback, float deadZone)
        {
            GetScope().BindAxis(action, callback, deadZone);
        }

        /// <summary>
        /// Gets or creates the input scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned input scope.</returns>
        private IInputScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<IInputService>(
                "Input is not configured. Call builder.UseInput() before binding actions.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }
    }

    /// <summary>
    /// Provides typed network operations whose ownership follows one module context.
    /// </summary>
    public sealed class NetworkCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific network scope.
        private INetworkScope mScope;

        /// <summary>
        /// Initializes network operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal NetworkCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Binds one typed network message callback.
        /// </summary>
        /// <typeparam name="T">The exact network message type.</typeparam>
        /// <param name="callback">The callback invoked on the game thread.</param>
        public void Bind<T>(Action<T> callback) where T : class
        {
            GetScope().Bind(callback);
        }

        /// <summary>
        /// Binds one network state callback.
        /// </summary>
        /// <param name="callback">The callback invoked after a state change.</param>
        public void BindState(Action<ENetworkState> callback)
        {
            GetScope().BindState(callback);
        }

        /// <summary>
        /// Sends one registered typed message.
        /// </summary>
        /// <typeparam name="T">The exact network message type.</typeparam>
        /// <param name="message">The message to encode and send.</param>
        /// <returns>A task completed after the frame is sent.</returns>
        public Task SendAsync<T>(T message) where T : class
        {
            return GetService().SendAsync(message);
        }

        /// <summary>
        /// Sends one request and awaits its correlated typed response.
        /// </summary>
        /// <typeparam name="TRequest">The request message type.</typeparam>
        /// <typeparam name="TResponse">The expected response message type.</typeparam>
        /// <param name="request">The request to encode and send.</param>
        /// <param name="timeout">The maximum response wait duration.</param>
        /// <param name="cancellationToken">Optional caller cancellation.</param>
        /// <returns>A task containing the correlated response.</returns>
        public Task<TResponse> RequestAsync<TRequest, TResponse>(
            TRequest request,
            TimeSpan timeout,
            CancellationToken cancellationToken)
            where TRequest : class, INetworkRequest
            where TResponse : class, INetworkResponse
        {
            return GetScope().RequestAsync<TRequest, TResponse>(
                request,
                timeout,
                cancellationToken);
        }

        /// <summary>
        /// Sends one generated request whose response type is declared by its contract.
        /// </summary>
        /// <typeparam name="TResponse">The declared response message type.</typeparam>
        /// <param name="request">The generated request to encode and send.</param>
        /// <param name="timeout">The maximum response wait duration.</param>
        /// <param name="cancellationToken">Optional caller cancellation.</param>
        /// <returns>A task containing the correlated response.</returns>
        public Task<TResponse> RequestAsync<TResponse>(
            INetworkRequest<TResponse> request,
            TimeSpan timeout,
            CancellationToken cancellationToken)
            where TResponse : class, INetworkResponse
        {
            return GetScope().RequestAsync(request, timeout, cancellationToken);
        }

        /// <summary>
        /// Connects the configured network transport.
        /// </summary>
        /// <param name="host">The remote host name or address.</param>
        /// <param name="port">The remote TCP port.</param>
        /// <returns>A task completed after connection.</returns>
        public Task ConnectAsync(string host, int port)
        {
            return GetService().ConnectAsync(host, port);
        }

        /// <summary>
        /// Disconnects the configured network transport.
        /// </summary>
        /// <returns>A task completed after disconnection.</returns>
        public Task DisconnectAsync()
        {
            return GetService().DisconnectAsync();
        }

        /// <summary>
        /// Gets the configured network service.
        /// </summary>
        /// <returns>The application network service.</returns>
        private INetworkService GetService()
        {
            return mContext.GetRequired<INetworkService>(
                "Networking is not configured. Call builder.UseNetwork() before using messages.");
        }

        /// <summary>
        /// Gets or creates the network scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned network scope.</returns>
        private INetworkScope GetScope()
        {
            if (mScope != null)
                return mScope;
            mScope = mContext.Scope.Own(GetService().CreateScope());
            return mScope;
        }
    }
}
