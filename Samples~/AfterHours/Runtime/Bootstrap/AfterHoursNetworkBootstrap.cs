using AfterHours.Generated.Network;
using Tritone.Kernel;
using Tritone.Messaging;
using Tritone.Networking;
using Tritone.Unity.Assets;
using UnityEngine;

namespace AfterHours.Bootstrap
{
    /// <summary>Optional network scene that sends one generated message without starting the RPG.</summary>
    public sealed class AfterHoursNetworkBootstrap : AfterHoursOptionalProfileBootstrap
    {
        private static int sPingNonce;
        [SerializeField] private bool mObserveProtocol;
        private INetworkService mNetwork;
        private INetworkScope mMessages;

        public bool ObservesProtocol => mObserveProtocol;
        protected override string LabId => mObserveProtocol ? "protocol-handshake-reconnect" : "NetworkDesk";

        protected override void Awake()
        {
            base.Awake();
            mNetwork = Application.Services.GetRequired<INetworkService>();
            if (mObserveProtocol)
                mNetwork.StateChanged += ShowConnectionState;
            else
            {
                mMessages = mNetwork.CreateScope();
                mMessages.Bind<CaseStatus>(message => SetResult($"Received {message.State} for case {message.CaseId}."));
            }
        }

        protected override void ConfigureProfile(GameApplicationBuilder builder)
        {
            var serializer = new MessageSerializer();
            NetworkMessages.Register(serializer);
            var options = new NetworkSessionOptions()
                .UseProtocolHandshake(NetworkMessages.Protocol, 5d)
                .UseHeartbeat<NightPing, NightPong>(() => new NightPing { Nonce = ++sPingNonce }, 3d, 9d)
                .UseReconnect(4, 1d, 1.8d, 8d);
            builder.UseAssets().UseTcpNetwork(serializer, maximumFrameSize: 1024 * 1024, options: options);
        }

        protected override async void RunProbe()
        {
            try
            {
                if (mNetwork.State is ENetworkState.Handshaking or ENetworkState.Connecting or ENetworkState.Reconnecting)
                    return;
                if (mNetwork.State != ENetworkState.Connected)
                    await mNetwork.ConnectAsync("127.0.0.1", 27831);
                if (mObserveProtocol)
                {
                    SetResult("Protocol accepted. Stop the relay to see reconnect attempts, then restart it.");
                    return;
                }
                SetResult("Sent a generated CaseStatus message. Waiting for the relay acknowledgement.");
                await mNetwork.SendAsync(new CaseStatus { CaseId = "last-stop", State = "SAMPLE_REQUEST" });
            }
            catch (System.Exception exception)
            {
                SetResult("OFFLINE / Start the optional local relay, then retry.\n" + exception.Message);
            }
        }

        protected override async void ResetProbe()
        {
            try
            {
                await mNetwork.DisconnectAsync();
                SetResult("Disconnected. Start the local relay before retrying.");
            }
            catch (System.Exception exception)
            {
                SetResult(exception.Message);
            }
        }

        private void ShowConnectionState(ENetworkState state) =>
            SetResult(state == ENetworkState.Connected ? "Connected. Generated protocol accepted." : $"Connection state: {state}");

        protected override void OnDestroy()
        {
            if (mNetwork != null)
                mNetwork.StateChanged -= ShowConnectionState;
            mMessages?.Dispose();
            base.OnDestroy();
        }
    }
}
