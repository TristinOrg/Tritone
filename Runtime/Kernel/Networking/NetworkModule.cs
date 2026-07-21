using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Kernel;
using Tritone.Messaging;

namespace Tritone.Networking
{
    /// <summary>
    /// Converts framed bytes to typed messages and dispatches them on the game thread.
    /// </summary>
    public sealed class NetworkModule : ModuleBase, INetworkService, IUpdateSystem
    {
        // Converts typed messages to deterministic binary frames.
        private readonly IMessageSerializer mSerializer;

        // Performs the replaceable physical transport work.
        private readonly INetworkTransport mTransport;

        // Manages optional protocol-specific heartbeat behavior.
        private readonly IHeartbeatSession mHeartbeat;

        // Configures bounded automatic reconnection.
        private readonly NetworkReconnectOptions mReconnect;

        /// <summary>
        /// Validates every new transport connection before application traffic begins.
        /// </summary>
        private readonly INetworkConnectionHandshake mHandshake;

        /// <summary>
        /// Cancels connection validation when the module stops.
        /// </summary>
        private readonly CancellationTokenSource mLifetimeCancellation = new();

        // Protects frames received from a background transport thread.
        private readonly object mQueueLock = new();

        // Stores frames waiting for game-thread dispatch.
        private readonly Queue<byte[]> mReceived = new();

        // Stores callbacks grouped by their exact message type.
        private readonly Dictionary<Type, List<NetworkBinding>> mBindings = new();

        // Stores pending request continuations by request identifier.
        private readonly Dictionary<int, PendingRequest> mRequests = new();

        // Produces positive request identifiers without reflection or random allocation.
        private int mNextRequestId;

        public ENetworkState State =>
            mHandshakeRunning
                ? ENetworkState.Handshaking
                : mReconnectPending || mReconnectRunning
                ? ENetworkState.Reconnecting
                : mTransport.State;
        public event Action<ENetworkState> StateChanged;
        public int Order => -900;

        public NetworkModule(IMessageSerializer serializer,
                             INetworkTransport transport,
                             NetworkSessionOptions options = null)
        {
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            mTransport  = transport ?? throw new ArgumentNullException(nameof(transport));
            mHeartbeat  = options?.CreateHeartbeat(mSerializer, mTransport);
            mReconnect  = options?.Reconnect;
            mHandshake  = options?.Handshake;
            mLastState  = State;
        }

        // Stores the last state delivered to listeners.
        private ENetworkState mLastState;

        // Stores the most recently requested endpoint.
        private string mHost;
        private int mPort;
        private bool mManualDisconnect;
        private bool mReconnectPending;
        private bool mReconnectRunning;
        private int mReconnectAttempts;
        private double mReconnectElapsed;
        private double mReconnectDelay;

        /// <summary>
        /// Stores whether connection validation is currently executing.
        /// </summary>
        private bool mHandshakeRunning;

        /// <summary>
        /// Stores whether the current transport connection passed validation.
        /// </summary>
        private bool mHandshakeComplete;

        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton<INetworkService>(this);
            mTransport.Received += OnReceived;
            mTransport.Faulted  += OnFaulted;
        }

        public INetworkScope CreateScope()
        {
            return new NetworkScope(this);
        }

        public async Task ConnectAsync(string host, int port)
        {
            mHost             = host;
            mPort             = port;
            mManualDisconnect = false;
            ResetReconnect();
            mHandshakeComplete = false;
            try
            {
                await mTransport.ConnectAsync(host, port);
                await ExecuteHandshakeAsync();
            }
            catch
            {
                await mTransport.DisconnectAsync();
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            mManualDisconnect = true;
            ResetReconnect();
            await mTransport.DisconnectAsync();
        }

        public Task SendAsync<T>(T message) where T : class
        {
            EnsureHandshakeComplete();
            return mTransport.SendAsync(mSerializer.Serialize(message));
        }

        public async Task<TResponse> RequestAsync<TRequest, TResponse>(
            TRequest request,
            TimeSpan timeout,
            CancellationToken cancellationToken)
            where TRequest : class, INetworkRequest
            where TResponse : class, INetworkResponse
        {
            return await RequestCoreAsync<TResponse>(request, timeout, cancellationToken);
        }

        public Task<TResponse> RequestAsync<TResponse>(
            INetworkRequest<TResponse> request,
            TimeSpan timeout,
            CancellationToken cancellationToken)
            where TResponse : class, INetworkResponse
        {
            return RequestCoreAsync<TResponse>(request, timeout, cancellationToken);
        }

        private async Task<TResponse> RequestCoreAsync<TResponse>(
            INetworkRequest request,
            TimeSpan timeout,
            CancellationToken cancellationToken)
            where TResponse : class, INetworkResponse
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));
            EnsureHandshakeComplete();

            var requestId = NextRequestId();
            request.RequestId = requestId;
            PendingRequest<TResponse> pending = new(requestId);
            mRequests.Add(requestId, pending);
            using (CancellationTokenSource timeoutSource = new(timeout))
            using (CancellationTokenSource linkedSource =
                   CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token,
                                                                   cancellationToken))
            using (linkedSource.Token.Register(pending.Cancel))
            {
                try
                {
                    await mTransport.SendAsync(mSerializer.SerializeObject(request));
                    return await pending.Task;
                }
                finally
                {
                    mRequests.Remove(requestId);
                }
            }
        }

        public void Update(in FrameTime time)
        {
            NotifyState();
            UpdateReconnect(time.UnscaledDeltaTime);
            mHeartbeat?.Update(time.UnscaledDeltaTime);
            while (true)
            {
                byte[] frame;
                lock (mQueueLock)
                {
                    if (mReceived.Count == 0)
                        return;
                    frame = mReceived.Dequeue();
                }
                Dispatch(mSerializer.Deserialize(frame));
            }
        }

        protected override void OnStop()
        {
            mTransport.Received -= OnReceived;
            mTransport.Faulted  -= OnFaulted;
            mLifetimeCancellation.Cancel();
            mBindings.Clear();
            StateChanged = null;
            mManualDisconnect = true;
            ResetReconnect();
            CancelRequests();
            lock (mQueueLock)
                mReceived.Clear();
            mTransport.Dispose();
            mLifetimeCancellation.Dispose();
        }

        internal void Add(NetworkBinding binding)
        {
            if (!mBindings.TryGetValue(binding.MessageType, out var bindings))
            {
                bindings = new List<NetworkBinding>();
                mBindings.Add(binding.MessageType, bindings);
            }
            bindings.Add(binding);
        }

        internal void Remove(NetworkBinding binding)
        {
            if (!mBindings.TryGetValue(binding.MessageType, out var bindings))
                return;
            bindings.Remove(binding);
            if (bindings.Count == 0)
                mBindings.Remove(binding.MessageType);
        }

        private void OnReceived(byte[] frame)
        {
            if (frame == null || (mHandshake != null && mHandshake.IsControlFrame(frame)))
                return;
            lock (mQueueLock)
                mReceived.Enqueue(frame);
        }

        private void OnFaulted(Exception exception)
        {
            Logger.Error("Network transport failed.", exception);
            BeginReconnect();
        }

        private void Dispatch(object message)
        {
            mHeartbeat?.Observe(message);
            if (message is INetworkResponse response &&
                mRequests.TryGetValue(response.RequestId, out var request))
            {
                request.Complete(message);
                return;
            }
            if (!mBindings.TryGetValue(message.GetType(), out var bindings))
                return;
            for (int i = bindings.Count - 1; i >= 0; i--)
                bindings[i].Invoke(message);
        }

        private void NotifyState()
        {
            if (mTransport.State == ENetworkState.Disconnected &&
                mLastState == ENetworkState.Connected)
            {
                mHandshakeComplete = false;
                BeginReconnect();
            }
            var state = State;
            if (state == mLastState)
                return;
            mLastState = state;
            StateChanged?.Invoke(state);
        }

        private void BeginReconnect()
        {
            if (mReconnect == null ||
                mManualDisconnect ||
                string.IsNullOrEmpty(mHost) ||
                mReconnectPending ||
                mReconnectRunning)
                return;
            mReconnectPending = true;
            mReconnectElapsed = 0.0;
            mReconnectDelay   = mReconnect.InitialDelay;
        }

        private void UpdateReconnect(double deltaTime)
        {
            if (!mReconnectPending || mReconnectRunning)
                return;
            mReconnectElapsed += deltaTime;
            if (mReconnectElapsed < mReconnectDelay)
                return;
            mReconnectPending = false;
            mReconnectRunning = true;
            _ = ReconnectAsync();
        }

        private async Task ReconnectAsync()
        {
            try
            {
                await mTransport.ConnectAsync(mHost, mPort);
                mHandshakeComplete = false;
                await ExecuteHandshakeAsync();
                ResetReconnect();
            }
            catch (NetworkProtocolHandshakeException exception)
            {
                Logger.Error("Network reconnect protocol handshake failed.", exception);
                mReconnectRunning = false;
                await mTransport.DisconnectAsync();
            }
            catch (Exception exception)
            {
                Logger.Error("Network reconnect attempt failed.", exception);
                await mTransport.DisconnectAsync();
                mReconnectAttempts++;
                mReconnectRunning = false;
                if (mReconnectAttempts >= mReconnect.MaximumAttempts)
                    return;
                mReconnectElapsed = 0.0;
                mReconnectDelay   = Math.Min(
                    mReconnect.MaximumDelay,
                    mReconnect.InitialDelay *
                    Math.Pow(mReconnect.DelayMultiplier, mReconnectAttempts));
                mReconnectPending = true;
            }
        }

        private void ResetReconnect()
        {
            mReconnectPending  = false;
            mReconnectRunning  = false;
            mReconnectAttempts = 0;
            mReconnectElapsed  = 0.0;
            mReconnectDelay    = 0.0;
        }

        /// <summary>
        /// Executes optional connection validation and marks application traffic as available.
        /// </summary>
        /// <returns>A task that completes when connection validation succeeds.</returns>
        private async Task ExecuteHandshakeAsync()
        {
            if (mHandshake == null)
            {
                mHandshakeComplete = true;
                return;
            }

            mHandshakeRunning = true;
            try
            {
                await mHandshake.ExecuteAsync(mTransport, mLifetimeCancellation.Token);
                mHandshakeComplete = true;
            }
            finally
            {
                mHandshakeRunning = false;
            }
        }

        /// <summary>
        /// Rejects application traffic until the configured connection handshake succeeds.
        /// </summary>
        private void EnsureHandshakeComplete()
        {
            if (mHandshake != null && !mHandshakeComplete)
                throw new InvalidOperationException("Network application traffic is unavailable before the connection handshake completes.");
        }

        private int NextRequestId()
        {
            do
            {
                mNextRequestId++;
                if (mNextRequestId <= 0)
                    mNextRequestId = 1;
            }
            while (mRequests.ContainsKey(mNextRequestId));
            return mNextRequestId;
        }

        private void CancelRequests()
        {
            foreach (var request in mRequests.Values)
                request.Cancel();
            mRequests.Clear();
        }
    }

    internal abstract class PendingRequest
    {
        internal abstract void Complete(object response);
        internal abstract void Cancel();
    }

    internal sealed class PendingRequest<TResponse> : PendingRequest
        where TResponse : class, INetworkResponse
    {
        private readonly TaskCompletionSource<TResponse> mCompletion = new();
        private readonly int mRequestId;

        internal Task<TResponse> Task => mCompletion.Task;

        internal PendingRequest(int requestId)
        {
            mRequestId = requestId;
        }

        internal override void Complete(object response)
        {
            if (response is TResponse typedResponse)
                mCompletion.TrySetResult(typedResponse);
            else
                mCompletion.TrySetException(
                    new InvalidOperationException(
                        $"Request '{mRequestId}' received an unexpected response type."));
        }

        internal override void Cancel()
        {
            mCompletion.TrySetCanceled();
        }
    }

    internal sealed class NetworkBinding
    {
        internal Type MessageType;
        internal Action<object> Callback;

        internal void Invoke(object message)
        {
            Callback.Invoke(message);
        }
    }
}
