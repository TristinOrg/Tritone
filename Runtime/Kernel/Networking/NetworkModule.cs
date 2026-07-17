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

        public ENetworkState State => mTransport.State;
        public event Action<ENetworkState> StateChanged;
        public int Order => -900;

        public NetworkModule(IMessageSerializer serializer,
                             INetworkTransport transport,
                             NetworkSessionOptions options = null)
        {
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            mTransport  = transport ?? throw new ArgumentNullException(nameof(transport));
            mHeartbeat  = options?.CreateHeartbeat(mSerializer, mTransport);
            mLastState  = mTransport.State;
        }

        // Stores the last state delivered to listeners.
        private ENetworkState mLastState;

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

        public Task ConnectAsync(string host, int port)
        {
            return mTransport.ConnectAsync(host, port);
        }

        public Task DisconnectAsync()
        {
            return mTransport.DisconnectAsync();
        }

        public Task SendAsync<T>(T message) where T : class
        {
            return mTransport.SendAsync(mSerializer.Serialize(message));
        }

        public async Task<TResponse> RequestAsync<TRequest, TResponse>(
            TRequest request,
            TimeSpan timeout,
            CancellationToken cancellationToken)
            where TRequest : class, INetworkRequest
            where TResponse : class, INetworkResponse
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));

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
                    await SendAsync(request);
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
            mBindings.Clear();
            StateChanged = null;
            CancelRequests();
            lock (mQueueLock)
                mReceived.Clear();
            mTransport.Dispose();
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
            if (frame == null)
                return;
            lock (mQueueLock)
                mReceived.Enqueue(frame);
        }

        private void OnFaulted(Exception exception)
        {
            Logger.Error("Network transport failed.", exception);
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
            var state = mTransport.State;
            if (state == mLastState)
                return;
            mLastState = state;
            StateChanged?.Invoke(state);
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
