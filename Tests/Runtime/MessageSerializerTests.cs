using System.IO;
using NUnit.Framework;
using Tritone.Messaging;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies explicit message codecs and strict frame validation.
    /// </summary>
    public sealed class MessageSerializerTests
    {
        [Test]
        public void Serialize_RoundTripsRegisteredMessage()
        {
            MessageSerializer serializer = CreateSerializer();
            TestMessage source = new()
            {
                Id     = 7,
                Name   = "Tritone",
                Weight = 1.5f
            };

            var frame  = serializer.Serialize(source);
            var result = (TestMessage)serializer.Deserialize(frame);

            Assert.AreEqual(source.Id, result.Id);
            Assert.AreEqual(source.Name, result.Name);
            Assert.AreEqual(source.Weight, result.Weight);
        }

        [Test]
        public void Deserialize_RejectsIncompleteFrame()
        {
            MessageSerializer serializer = CreateSerializer();

            Assert.Throws<EndOfStreamException>(
                () => serializer.Deserialize(new byte[] { 1, 0, 0, 0 }));
        }

        [Test]
        public void Deserialize_RejectsTrailingBytes()
        {
            MessageSerializer serializer = CreateSerializer();
            var frame                     = serializer.Serialize(new TestMessage());
            var invalid                   = new byte[frame.Length + 1];
            System.Buffer.BlockCopy(frame, 0, invalid, 0, frame.Length);

            Assert.Throws<InvalidDataException>(() => serializer.Deserialize(invalid));
        }

        private static MessageSerializer CreateSerializer()
        {
            MessageSerializer serializer = new();
            serializer.Register(1, new TestMessageCodec());
            return serializer;
        }

        private sealed class TestMessage
        {
            internal int Id;
            internal string Name;
            internal float Weight;
        }

        private sealed class TestMessageCodec : IMessageCodec<TestMessage>
        {
            public void Write(MessageWriter writer, TestMessage message)
            {
                writer.WriteInt32(message.Id);
                writer.WriteString(message.Name);
                writer.WriteSingle(message.Weight);
            }

            public TestMessage Read(MessageReader reader)
            {
                return new TestMessage
                {
                    Id     = reader.ReadInt32(),
                    Name   = reader.ReadString(),
                    Weight = reader.ReadSingle()
                };
            }
        }
    }
}
