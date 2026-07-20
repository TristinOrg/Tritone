using System;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Networking;

namespace Tritone.Kernel
{
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
