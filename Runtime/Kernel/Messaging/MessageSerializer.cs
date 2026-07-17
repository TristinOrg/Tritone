using System;
using System.Collections.Generic;
using System.IO;

namespace Tritone.Messaging
{
    /// <summary>
    /// Serializes explicitly registered message types without field reflection.
    /// </summary>
    public sealed class MessageSerializer : IMessageSerializer
    {
        private readonly Dictionary<Type, ICodecAdapter> mByType = new();
        private readonly Dictionary<int, ICodecAdapter> mById = new();

        public void Register<T>(int typeId, IMessageCodec<T> codec) where T : class
        {
            if (typeId <= 0)
                throw new ArgumentOutOfRangeException(nameof(typeId));
            if (codec == null)
                throw new ArgumentNullException(nameof(codec));
            var type = typeof(T);
            if (mByType.ContainsKey(type) || mById.ContainsKey(typeId))
                throw new InvalidOperationException("Message type and type ID registrations must be unique.");
            CodecAdapter<T> adapter = new(typeId, codec);
            mByType.Add(type, adapter);
            mById.Add(typeId, adapter);
        }

        public byte[] Serialize<T>(T message) where T : class
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (!mByType.TryGetValue(typeof(T), out var adapter))
                throw new InvalidOperationException($"Message '{typeof(T).FullName}' is not registered.");
            MessageWriter writer = new();
            writer.WriteInt32(adapter.TypeId);
            adapter.Write(writer, message);
            return writer.ToArray();
        }

        public object Deserialize(byte[] frame)
        {
            if (frame == null || frame.Length < 4)
                throw new InvalidDataException("A message frame must contain a positive type ID.");
            MessageReader reader = new(frame);
            var typeId = reader.ReadInt32();
            if (!mById.TryGetValue(typeId, out var adapter))
                throw new InvalidDataException($"Message type ID '{typeId}' is not registered.");
            var message = adapter.Read(reader);
            if (reader.Remaining != 0)
                throw new InvalidDataException("The message frame contains unread trailing bytes.");
            return message;
        }

        public bool TryGetMessageType(int typeId, out Type messageType)
        {
            if (mById.TryGetValue(typeId, out var adapter))
            {
                messageType = adapter.MessageType;
                return true;
            }
            messageType = null;
            return false;
        }

        private interface ICodecAdapter
        {
            int TypeId { get; }
            Type MessageType { get; }
            void Write(MessageWriter writer, object message);
            object Read(MessageReader reader);
        }

        private sealed class CodecAdapter<T> : ICodecAdapter where T : class
        {
            private readonly IMessageCodec<T> mCodec;
            public int TypeId { get; }
            public Type MessageType => typeof(T);

            internal CodecAdapter(int typeId, IMessageCodec<T> codec)
            {
                TypeId = typeId;
                mCodec = codec;
            }

            public void Write(MessageWriter writer, object message)
            {
                mCodec.Write(writer, (T)message);
            }

            public object Read(MessageReader reader)
            {
                return mCodec.Read(reader);
            }
        }
    }
}
