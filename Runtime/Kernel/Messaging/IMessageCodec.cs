namespace Tritone.Messaging
{
    /// <summary>
    /// Explicitly encodes and decodes one network message type.
    /// </summary>
    public interface IMessageCodec<T> where T : class
    {
        /// <summary>
        /// Writes one message payload.
        /// </summary>
        void Write(MessageWriter writer, T message);

        /// <summary>
        /// Reads one complete message payload.
        /// </summary>
        T Read(MessageReader reader);
    }
}
