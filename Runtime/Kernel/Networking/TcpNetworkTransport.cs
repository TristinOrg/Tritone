using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Networking
{
    /// <summary>
    /// Transfers length-prefixed message frames over one TCP connection.
    /// </summary>
    public sealed class TcpNetworkTransport : INetworkTransport
    {
        // Prevents concurrent writes from interleaving.
        private readonly SemaphoreSlim mSendLock = new(1, 1);

        // Rejects unexpectedly large incoming frames.
        private readonly int mMaximumFrameSize;

        private TcpClient mClient;
        private NetworkStream mStream;
        private CancellationTokenSource mCancellation;

        public ENetworkState State { get; private set; }
        public event Action<byte[]> Received;
        public event Action<Exception> Faulted;

        public TcpNetworkTransport(int maximumFrameSize = 4 * 1024 * 1024)
        {
            if (maximumFrameSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumFrameSize));
            mMaximumFrameSize = maximumFrameSize;
        }

        public async Task ConnectAsync(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("A network host is required.", nameof(host));
            if (port <= 0 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port));
            if (State != ENetworkState.Disconnected)
                throw new InvalidOperationException("The network transport is already active.");

            State         = ENetworkState.Connecting;
            mClient       = new TcpClient();
            mCancellation = new CancellationTokenSource();
            try
            {
                await mClient.ConnectAsync(host, port);
                mStream = mClient.GetStream();
                State   = ENetworkState.Connected;
                _ = ReceiveLoopAsync(mCancellation.Token);
            }
            catch
            {
                DisposeConnection();
                throw;
            }
        }

        public async Task SendAsync(byte[] frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            if (State != ENetworkState.Connected)
                throw new InvalidOperationException("The network transport is not connected.");
            if (frame.Length > mMaximumFrameSize)
                throw new InvalidDataException("The outgoing network frame exceeds the configured limit.");

            var header = new byte[4];
            WriteLength(header, frame.Length);
            await mSendLock.WaitAsync();
            try
            {
                await mStream.WriteAsync(header, 0, header.Length);
                await mStream.WriteAsync(frame, 0, frame.Length);
            }
            finally
            {
                mSendLock.Release();
            }
        }

        public Task DisconnectAsync()
        {
            if (State == ENetworkState.Disconnected)
                return Task.CompletedTask;
            State = ENetworkState.Disconnecting;
            DisposeConnection();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            DisposeConnection();
            mSendLock.Dispose();
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var header = new byte[4];
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await ReadExactlyAsync(header, cancellationToken);
                    var count = ReadLength(header);
                    if (count <= 0 || count > mMaximumFrameSize)
                        throw new InvalidDataException("The incoming network frame has an invalid length.");
                    var frame = new byte[count];
                    await ReadExactlyAsync(frame, cancellationToken);
                    Received?.Invoke(frame);
                }
            }
            catch (Exception exception)
            {
                if (!cancellationToken.IsCancellationRequested)
                    Faulted?.Invoke(exception);
            }
            finally
            {
                DisposeConnection();
            }
        }

        private async Task ReadExactlyAsync(byte[] buffer, CancellationToken cancellationToken)
        {
            var offset = 0;
            while (offset < buffer.Length)
            {
                var count = await mStream.ReadAsync(buffer,
                                                    offset,
                                                    buffer.Length - offset,
                                                    cancellationToken);
                if (count == 0)
                    throw new EndOfStreamException("The remote endpoint closed the connection.");
                offset += count;
            }
        }

        private void DisposeConnection()
        {
            mCancellation?.Cancel();
            mStream?.Dispose();
            mClient?.Close();
            mCancellation?.Dispose();
            mCancellation = null;
            mStream       = null;
            mClient       = null;
            State         = ENetworkState.Disconnected;
        }

        private static int ReadLength(byte[] header)
        {
            return header[0] |
                   header[1] << 8 |
                   header[2] << 16 |
                   header[3] << 24;
        }

        private static void WriteLength(byte[] header, int value)
        {
            header[0] = (byte)value;
            header[1] = (byte)(value >> 8);
            header[2] = (byte)(value >> 16);
            header[3] = (byte)(value >> 24);
        }
    }
}
