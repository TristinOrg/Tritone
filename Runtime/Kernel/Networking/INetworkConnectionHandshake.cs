using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Networking
{
    /// <summary>
    /// Validates one connected transport before application messages may be exchanged.
    /// </summary>
    public interface INetworkConnectionHandshake
    {
        /// <summary>
        /// Determines whether a received frame belongs to this handshake rather than application messaging.
        /// </summary>
        /// <param name="frame">The received transport frame.</param>
        /// <returns>True when the frame must not reach the application serializer.</returns>
        bool IsControlFrame(byte[] frame);

        /// <summary>
        /// Executes one connection handshake over an already connected transport.
        /// </summary>
        /// <param name="transport">The connected transport used to exchange control frames.</param>
        /// <param name="cancellationToken">The token used to cancel application shutdown.</param>
        /// <returns>A task that completes only after the connection is validated.</returns>
        Task ExecuteAsync(INetworkTransport transport, CancellationToken cancellationToken);
    }
}
