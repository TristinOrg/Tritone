using System;
using System.Threading.Tasks;

namespace Tritone.Networking
{
    /// <summary>
    /// Provides replaceable framed byte transport for the networking module.
    /// </summary>
    public interface INetworkTransport : IDisposable
    {
        ENetworkState State { get; }
        event Action<byte[]> Received;
        event Action<Exception> Faulted;
        Task ConnectAsync(string host, int port);
        Task SendAsync(byte[] frame);
        Task DisconnectAsync();
    }
}
