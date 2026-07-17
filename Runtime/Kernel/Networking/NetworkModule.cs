using System;
using System.Collections.Generic;
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

        // Protects frames received from a background transport thread.
        private readonly object mQueueLock = new();

        // Stores frames waiting for game-thread dispatch.
        private readonly Queue<byte[]> mReceived = new();

        // Stores callbacks grouped by their exact message type.
        private readonly Dictionary<Type, List<NetworkBinding>> mBindings = new();

        public ENetworkState State => mTransport.State;
        public int Order => -900;

        public NetworkModule(IMessageSerializer serializer, INetworkTransport transport)
        {
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            mTransport  = transport ?? throw new ArgumentNullException(nameof(transport));
        }

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

        public void Update(in FrameTime time)
        {
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
            if (!mBindings.TryGetValue(message.GetType(), out var bindings))
                return;
            for (int i = bindings.Count - 1; i >= 0; i--)
                bindings[i].Invoke(message);
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
