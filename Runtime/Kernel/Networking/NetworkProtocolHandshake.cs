using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Networking
{
    /// <summary>
    /// Exchanges generated protocol descriptors and rejects incompatible connections.
    /// </summary>
    public sealed class NetworkProtocolHandshake : INetworkConnectionHandshake
    {
        /// <summary>
        /// Stores the local generated protocol descriptor.
        /// </summary>
        private readonly NetworkProtocolDescriptor mLocalProtocol;

        /// <summary>
        /// Stores the maximum response wait duration.
        /// </summary>
        private readonly TimeSpan mTimeout;

        /// <summary>
        /// Stores the active response completion while one handshake is running.
        /// </summary>
        private TaskCompletionSource<NetworkProtocolHandshakeResponse> mCompletion;

        /// <summary>
        /// Initializes one reusable protocol connection handshake.
        /// </summary>
        /// <param name="localProtocol">The local generated protocol descriptor.</param>
        /// <param name="timeout">The maximum server response wait duration.</param>
        public NetworkProtocolHandshake(in NetworkProtocolDescriptor localProtocol, TimeSpan timeout)
        {
            if (!localProtocol.IsValid)
                throw new ArgumentException("A valid local protocol descriptor is required.", nameof(localProtocol));
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            mLocalProtocol = localProtocol;
            mTimeout       = timeout;
        }

        /// <inheritdoc />
        public bool IsControlFrame(byte[] frame)
        {
            return NetworkProtocolHandshakeFrame.IsFrame(frame);
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(INetworkTransport transport, CancellationToken cancellationToken)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (mCompletion != null)
                throw new InvalidOperationException("A protocol handshake is already running.");

            mCompletion = new TaskCompletionSource<NetworkProtocolHandshakeResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            transport.Received += OnReceived;
            try
            {
                await transport.SendAsync(NetworkProtocolHandshakeFrame.CreateHello(in mLocalProtocol));
                var responseTask = mCompletion.Task;
                using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var timeoutTask = Task.Delay(mTimeout, timeoutSource.Token);
                var completedTask = await Task.WhenAny(responseTask, timeoutTask);
                if (completedTask != responseTask)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException("The network protocol handshake response timed out.");
                }

                timeoutSource.Cancel();
                var response = await responseTask;
                var compatibility = response.Compatibility;
                var remoteProtocol = response.RemoteProtocol;
                if (compatibility == ENetworkProtocolCompatibility.Compatible)
                    compatibility = mLocalProtocol.EvaluateCompatibility(in remoteProtocol);
                if (compatibility != ENetworkProtocolCompatibility.Compatible)
                    throw new NetworkProtocolHandshakeException(compatibility, in remoteProtocol);
            }
            finally
            {
                transport.Received -= OnReceived;
                mCompletion = null;
            }
        }

        /// <summary>
        /// Completes the active handshake when a valid server response arrives.
        /// </summary>
        /// <param name="frame">The received transport frame.</param>
        private void OnReceived(byte[] frame)
        {
            if (mCompletion != null && NetworkProtocolHandshakeFrame.TryReadResponse(frame, out var response))
                mCompletion.TrySetResult(response);
        }

    }
}
