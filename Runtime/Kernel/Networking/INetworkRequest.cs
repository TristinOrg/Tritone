namespace Tritone.Networking
{
    /// <summary>
    /// Identifies a message that expects one correlated response.
    /// </summary>
    public interface INetworkRequest
    {
        /// <summary>
        /// Gets or sets the request identifier assigned by Tritone.
        /// </summary>
        int RequestId { get; set; }
    }

    /// <summary>
    /// Declares the response type expected by one generated request message.
    /// </summary>
    /// <typeparam name="TResponse">The correlated response message type.</typeparam>
    public interface INetworkRequest<TResponse> : INetworkRequest
        where TResponse : class, INetworkResponse
    {
    }

    /// <summary>
    /// Identifies a response correlated with one request.
    /// </summary>
    public interface INetworkResponse
    {
        /// <summary>
        /// Gets the matching request identifier.
        /// </summary>
        int RequestId { get; }
    }
}
