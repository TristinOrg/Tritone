using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Networking
{
    /// <summary>
    /// Sends typed messages and creates lifecycle-owned receive scopes.
    /// </summary>
    public interface INetworkService
    {
        ENetworkState State { get; }
        event Action<ENetworkState> StateChanged;
        INetworkScope CreateScope();
        Task ConnectAsync(string host, int port);
        Task DisconnectAsync();
        Task SendAsync<T>(T message) where T : class;
        Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request,
                                                         TimeSpan timeout,
                                                         CancellationToken cancellationToken)
            where TRequest : class, INetworkRequest
            where TResponse : class, INetworkResponse;
        Task<TResponse> RequestAsync<TResponse>(INetworkRequest<TResponse> request,
                                               TimeSpan timeout,
                                               CancellationToken cancellationToken)
            where TResponse : class, INetworkResponse;
    }

    /// <summary>
    /// Owns typed message callbacks for one module lifetime.
    /// </summary>
    public interface INetworkScope : IDisposable
    {
        void Bind<T>(Action<T> callback) where T : class;
        void BindState(Action<ENetworkState> callback);
        Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request,
                                                         TimeSpan timeout,
                                                         CancellationToken cancellationToken = default)
            where TRequest : class, INetworkRequest
            where TResponse : class, INetworkResponse;
        Task<TResponse> RequestAsync<TResponse>(INetworkRequest<TResponse> request,
                                               TimeSpan timeout,
                                               CancellationToken cancellationToken = default)
            where TResponse : class, INetworkResponse;
    }
}
