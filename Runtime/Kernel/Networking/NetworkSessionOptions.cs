using System;
using System.Threading.Tasks;
using Tritone.Messaging;

namespace Tritone.Networking
{
    /// <summary>
    /// Configures optional protocol-specific behavior for one network session.
    /// </summary>
    public sealed class NetworkSessionOptions
    {
        // Creates the configured heartbeat behavior.
        private Func<IMessageSerializer, INetworkTransport, IHeartbeatSession> mHeartbeatFactory;

        /// <summary>
        /// Configures heartbeat request and response message types.
        /// </summary>
        public NetworkSessionOptions UseHeartbeat<TPing, TPong>(Func<TPing> pingFactory,
                                                               double intervalSeconds = 10.0,
                                                               double timeoutSeconds = 30.0)
            where TPing : class
            where TPong : class
        {
            if (pingFactory == null)
                throw new ArgumentNullException(nameof(pingFactory));
            if (intervalSeconds <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            if (timeoutSeconds <= intervalSeconds)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            if (mHeartbeatFactory != null)
                throw new InvalidOperationException("Heartbeat is already configured.");

            mHeartbeatFactory = (serializer, transport) =>
                new HeartbeatSession<TPing, TPong>(serializer,
                                                   transport,
                                                   pingFactory,
                                                   intervalSeconds,
                                                   timeoutSeconds);
            return this;
        }

        internal IHeartbeatSession CreateHeartbeat(IMessageSerializer serializer,
                                                   INetworkTransport transport)
        {
            return mHeartbeatFactory?.Invoke(serializer, transport);
        }
    }

    internal interface IHeartbeatSession
    {
        void Update(double deltaTime);
        void Observe(object message);
        void Reset();
    }

    internal sealed class HeartbeatSession<TPing, TPong> : IHeartbeatSession
        where TPing : class
        where TPong : class
    {
        private readonly IMessageSerializer mSerializer;
        private readonly INetworkTransport mTransport;
        private readonly Func<TPing> mPingFactory;
        private readonly double mInterval;
        private readonly double mTimeout;

        private double mSendElapsed;
        private double mReceiveElapsed;
        private bool mSending;

        internal HeartbeatSession(IMessageSerializer serializer,
                                  INetworkTransport transport,
                                  Func<TPing> pingFactory,
                                  double interval,
                                  double timeout)
        {
            mSerializer  = serializer;
            mTransport   = transport;
            mPingFactory = pingFactory;
            mInterval    = interval;
            mTimeout     = timeout;
        }

        public void Update(double deltaTime)
        {
            if (mTransport.State != ENetworkState.Connected)
            {
                Reset();
                return;
            }

            mSendElapsed    += deltaTime;
            mReceiveElapsed += deltaTime;
            if (mReceiveElapsed >= mTimeout)
            {
                _ = mTransport.DisconnectAsync();
                Reset();
                return;
            }
            if (mSendElapsed < mInterval || mSending)
                return;

            mSendElapsed = 0.0;
            mSending     = true;
            _ = SendAsync();
        }

        public void Observe(object message)
        {
            if (message is TPong)
                mReceiveElapsed = 0.0;
        }

        public void Reset()
        {
            mSendElapsed    = 0.0;
            mReceiveElapsed = 0.0;
            mSending        = false;
        }

        private async Task SendAsync()
        {
            try
            {
                await mTransport.SendAsync(mSerializer.Serialize(mPingFactory.Invoke()));
            }
            catch
            {
                await mTransport.DisconnectAsync();
            }
            finally
            {
                mSending = false;
            }
        }
    }
}
