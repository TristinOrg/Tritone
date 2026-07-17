using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Networking
{
    /// <summary>
    /// Releases all message callbacks registered by one owner.
    /// </summary>
    internal sealed class NetworkScope : INetworkScope
    {
        private NetworkModule mModule;
        private readonly List<NetworkBinding> mBindings = new();
        private readonly CancellationTokenSource mCancellation = new();

        internal NetworkScope(NetworkModule module)
        {
            mModule = module;
        }

        public void Bind<T>(Action<T> callback) where T : class
        {
            if (mModule == null)
                throw new ObjectDisposedException(nameof(NetworkScope));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            NetworkBinding binding = new()
            {
                MessageType = typeof(T),
                Callback    = message => callback((T)message)
            };
            mBindings.Add(binding);
            mModule.Add(binding);
        }

        public void Dispose()
        {
            if (mModule == null)
                return;
            for (int i = mBindings.Count - 1; i >= 0; i--)
                mModule.Remove(mBindings[i]);
            mCancellation.Cancel();
            mCancellation.Dispose();
            mBindings.Clear();
            mModule = null;
        }

        public Task<TResponse> RequestAsync<TRequest, TResponse>(
            TRequest request,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
            where TRequest : class, INetworkRequest
            where TResponse : class, INetworkResponse
        {
            if (mModule == null)
                throw new ObjectDisposedException(nameof(NetworkScope));
            if (!cancellationToken.CanBeCanceled)
                return mModule.RequestAsync<TRequest, TResponse>(request,
                                                                 timeout,
                                                                 mCancellation.Token);
            return RequestLinkedAsync<TRequest, TResponse>(request, timeout, cancellationToken);
        }

        private async Task<TResponse> RequestLinkedAsync<TRequest, TResponse>(
            TRequest request,
            TimeSpan timeout,
            CancellationToken cancellationToken)
            where TRequest : class, INetworkRequest
            where TResponse : class, INetworkResponse
        {
            using (CancellationTokenSource linkedSource =
                   CancellationTokenSource.CreateLinkedTokenSource(mCancellation.Token,
                                                                   cancellationToken))
                return await mModule.RequestAsync<TRequest, TResponse>(request,
                                                                       timeout,
                                                                       linkedSource.Token);
        }
    }
}
