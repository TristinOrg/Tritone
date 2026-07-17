using System;
using System.Threading.Tasks;

namespace Tritone.Networking
{
    /// <summary>
    /// Sends typed messages and creates lifecycle-owned receive scopes.
    /// </summary>
    public interface INetworkService
    {
        ENetworkState State { get; }
        INetworkScope CreateScope();
        Task ConnectAsync(string host, int port);
        Task DisconnectAsync();
        Task SendAsync<T>(T message) where T : class;
    }

    /// <summary>
    /// Owns typed message callbacks for one module lifetime.
    /// </summary>
    public interface INetworkScope : IDisposable
    {
        void Bind<T>(Action<T> callback) where T : class;
    }
}
