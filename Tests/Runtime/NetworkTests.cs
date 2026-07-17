using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Tritone.Kernel;
using Tritone.Messaging;
using Tritone.Networking;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies typed network dispatch, sending, and module-owned cleanup.
    /// </summary>
    public sealed class NetworkTests
    {
        [Test]
        public void Network_DispatchesOnUpdateAndSerializesSend()
        {
            MessageSerializer serializer = new();
            serializer.Register(1, new PingCodec());
            TestTransport transport = new();
            NetworkConsumer consumer = new();
            var application = new GameApplicationBuilder()
                .UseNetwork(serializer, transport)
                .AddModule(consumer)
                .Build();
            application.Start();

            transport.Push(serializer.Serialize(new Ping { Value = 9 }));
            Assert.AreEqual(0, consumer.Value);
            FrameTime time = new(0.016, 0.016, 0.016, 0);
            application.Update(in time);

            Assert.AreEqual(9, consumer.Value);
            consumer.Send().GetAwaiter().GetResult();
            Assert.AreEqual(10, ((Ping)serializer.Deserialize(transport.LastSent)).Value);
            application.Stop();
            Assert.IsTrue(transport.Disposed);
        }

        private sealed class NetworkConsumer : ModuleBase
        {
            internal int Value;

            protected override void OnStart()
            {
                BindMessage<Ping>(message => Value = message.Value);
            }

            internal Task Send()
            {
                return SendMessageAsync(new Ping { Value = 10 });
            }
        }

        private sealed class Ping
        {
            internal int Value;
        }

        private sealed class PingCodec : IMessageCodec<Ping>
        {
            public void Write(MessageWriter writer, Ping message)
            {
                writer.WriteInt32(message.Value);
            }

            public Ping Read(MessageReader reader)
            {
                return new Ping { Value = reader.ReadInt32() };
            }
        }

        private sealed class TestTransport : INetworkTransport
        {
            internal byte[] LastSent;
            internal bool Disposed;

            public ENetworkState State => ENetworkState.Connected;
            public event Action<byte[]> Received;
            public event Action<Exception> Faulted;

            public Task ConnectAsync(string host, int port)
            {
                return Task.CompletedTask;
            }

            public Task SendAsync(byte[] frame)
            {
                LastSent = frame;
                return Task.CompletedTask;
            }

            public Task DisconnectAsync()
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
                Disposed = true;
            }

            internal void Push(byte[] frame)
            {
                Received?.Invoke(frame);
            }
        }
    }
}
