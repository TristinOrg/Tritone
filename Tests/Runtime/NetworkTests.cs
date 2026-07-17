using System;
using System.Threading;
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

        [Test]
        public void RequestAsync_CompletesMatchingResponse()
        {
            MessageSerializer serializer = new();
            serializer.Register(1, new RequestCodec());
            serializer.Register(2, new ResponseCodec());
            TestTransport transport = new();
            RequestConsumer consumer = new();
            var application = new GameApplicationBuilder()
                .UseNetwork(serializer, transport)
                .AddModule(consumer)
                .Build();
            application.Start();

            var requestTask = consumer.Request();
            var request     = (TestRequest)serializer.Deserialize(transport.LastSent);
            transport.Push(serializer.Serialize(new TestResponse
            {
                RequestId = request.RequestId,
                Value     = 42
            }));
            FrameTime time = new(0.016, 0.016, 0.016, 0);
            application.Update(in time);

            Assert.AreEqual(42, requestTask.GetAwaiter().GetResult().Value);
            application.Stop();
        }

        [Test]
        public void Session_SendsHeartbeatAndPublishesState()
        {
            MessageSerializer serializer = new();
            serializer.Register(1, new PingCodec());
            TestTransport transport = new();
            SessionConsumer consumer = new();
            NetworkSessionOptions options = new();
            options.UseHeartbeat<Ping, Ping>(() => new Ping { Value = 1 }, 0.5, 1.5);
            var application = new GameApplicationBuilder()
                .UseNetwork(serializer, transport, options)
                .AddModule(consumer)
                .Build();
            application.Start();

            FrameTime time = new(0.6, 0.6, 0.6, 0);
            application.Update(in time);
            Assert.AreEqual(1, ((Ping)serializer.Deserialize(transport.LastSent)).Value);

            transport.CurrentState = ENetworkState.Disconnected;
            application.Update(in time);
            Assert.AreEqual(ENetworkState.Disconnected, consumer.State);
            application.Stop();
        }

        [Test]
        public void Session_ReconnectsUnexpectedDisconnectOnly()
        {
            MessageSerializer serializer = new();
            TestTransport transport = new();
            SessionConsumer consumer = new();
            NetworkSessionOptions options = new();
            options.UseReconnect(3, 0.0, 2.0, 1.0);
            var application = new GameApplicationBuilder()
                .UseNetwork(serializer, transport, options)
                .AddModule(consumer)
                .Build();
            application.Start();
            consumer.Connect().GetAwaiter().GetResult();

            transport.CurrentState = ENetworkState.Disconnected;
            FrameTime time = new(0.1, 0.1, 0.1, 0);
            application.Update(in time);
            Assert.AreEqual(2, transport.ConnectCount);

            consumer.Disconnect().GetAwaiter().GetResult();
            application.Update(in time);
            Assert.AreEqual(2, transport.ConnectCount);
            application.Stop();
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

        private sealed class RequestConsumer : ModuleBase
        {
            internal Task<TestResponse> Request()
            {
                return RequestAsync(new TestRequest());
            }
        }

        private sealed class SessionConsumer : ModuleBase
        {
            internal ENetworkState State = ENetworkState.Connected;

            protected override void OnStart()
            {
                BindNetworkState(state => State = state);
            }

            internal Task Connect()
            {
                return ConnectNetworkAsync("localhost", 9000);
            }

            internal Task Disconnect()
            {
                return DisconnectNetworkAsync();
            }
        }

        private sealed class TestRequest : INetworkRequest<TestResponse>
        {
            public int RequestId { get; set; }
        }

        private sealed class TestResponse : INetworkResponse
        {
            public int RequestId { get; set; }
            internal int Value;
        }

        private sealed class RequestCodec : IMessageCodec<TestRequest>
        {
            public void Write(MessageWriter writer, TestRequest message)
            {
                writer.WriteInt32(message.RequestId);
            }

            public TestRequest Read(MessageReader reader)
            {
                return new TestRequest { RequestId = reader.ReadInt32() };
            }
        }

        private sealed class ResponseCodec : IMessageCodec<TestResponse>
        {
            public void Write(MessageWriter writer, TestResponse message)
            {
                writer.WriteInt32(message.RequestId);
                writer.WriteInt32(message.Value);
            }

            public TestResponse Read(MessageReader reader)
            {
                return new TestResponse
                {
                    RequestId = reader.ReadInt32(),
                    Value     = reader.ReadInt32()
                };
            }
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
            internal int ConnectCount;

            internal ENetworkState CurrentState = ENetworkState.Connected;

            public ENetworkState State => CurrentState;
            public event Action<byte[]> Received;
            public event Action<Exception> Faulted;

            public Task ConnectAsync(string host, int port)
            {
                ConnectCount++;
                CurrentState = ENetworkState.Connected;
                return Task.CompletedTask;
            }

            public Task SendAsync(byte[] frame)
            {
                LastSent = frame;
                return Task.CompletedTask;
            }

            public Task DisconnectAsync()
            {
                CurrentState = ENetworkState.Disconnected;
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
