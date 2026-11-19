using AfterHours.Generated.Network;
using Tritone.Kernel;
using Tritone.Messaging;
using Tritone.Networking;
using Tritone.Unity.Assets;

namespace AfterHours.Bootstrap
{
    /// <summary>Optional network scene that sends one generated message without starting the RPG.</summary>
    public sealed class AfterHoursNetworkBootstrap : AfterHoursOptionalProfileBootstrap
    {
        private static int sPingNonce;

        protected override string LabId => "NetworkDesk";

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
                var network = Application.Services.GetRequired<INetworkService>();
                await network.ConnectAsync("127.0.0.1", 27831);
                await network.SendAsync(new CaseStatus { CaseId = "last-stop", State = "SAMPLE_REQUEST" });
                SetResult("CONNECTED / generated CaseStatus message sent to the local relay.");
            }
            catch (System.Exception exception)
            {
                SetResult("OFFLINE / Start the optional local relay, then retry.\n" + exception.Message);
            }
        }

        protected override async void ResetProbe()
        {
            try { await Application.Services.GetRequired<INetworkService>().DisconnectAsync(); }
            catch { }
            SetResult("READY / Start the optional local relay, then click Send Request.");
        }
    }
}
