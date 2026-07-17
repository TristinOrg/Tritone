namespace Tritone.Messaging
{
    /// <summary>
    /// Registers explicit message codecs and converts typed messages to framed bytes.
    /// </summary>
    public interface IMessageSerializer
    {
        void Register<T>(int typeId, IMessageCodec<T> codec) where T : class;
        byte[] Serialize<T>(T message) where T : class;
        object Deserialize(byte[] frame);
        bool TryGetMessageType(int typeId, out System.Type messageType);
    }
}
