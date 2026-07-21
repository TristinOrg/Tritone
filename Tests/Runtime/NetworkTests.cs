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

        /// <summary>
        /// Verifies that connect completes only after a compatible protocol response arrives.
        /// </summary>
        [Test]
        public void Session_CompletesProtocolHandshakeBeforeConnectReturns()
        {
            MessageSerializer serializer = new();
            TestTransport transport = new();
            var protocol = new NetworkProtocolDescriptor("game", 1, 2, 1, "schema-v2");
            transport.HandshakeProtocol = protocol;
            NetworkSessionOptions options = new();
            options.UseProtocolHandshake(in protocol);
            var application = new GameApplicationBuilder()
                .UseNetwork(serializer, transport, options)
                .Build();
            application.Start();

            application.Services.GetRequired<INetworkService>().ConnectAsync("localhost", 9000).GetAwaiter().GetResult();

            Assert.AreEqual(1, transport.HandshakeCount);
            Assert.AreEqual(ENetworkState.Connected, transport.State);
            application.Stop();
        }

        /// <summary>
        /// Verifies that an incompatible authoritative response disconnects and reports its reason.
        /// </summary>
        [Test]
        public void Session_RejectsIncompatibleProtocolAndDisconnects()
        {
            MessageSerializer serializer = new();
            TestTransport transport = new();
            var localProtocol = new NetworkProtocolDescriptor("game", 1, 2, 1, "schema-v2");
            transport.HandshakeProtocol = new NetworkProtocolDescriptor("game", 2, 0, 0, "schema-v3");
            NetworkSessionOptions options = new();
            options.UseProtocolHandshake(in localProtocol);
            var application = new GameApplicationBuilder()
                .UseNetwork(serializer, transport, options)
                .Build();
            application.Start();

            NetworkProtocolHandshakeException failure = null;
            try
            {
                application.Services.GetRequired<INetworkService>().ConnectAsync("localhost", 9000).GetAwaiter().GetResult();
                Assert.Fail("An incompatible protocol should reject the connection.");
            }
            catch (NetworkProtocolHandshakeException exception)
            {
                failure = exception;
            }

            Assert.AreEqual(ENetworkProtocolCompatibility.MajorVersionMismatch, failure?.Compatibility);
            Assert.AreEqual(ENetworkState.Disconnected, transport.State);
            application.Stop();
        }

        /// <summary>
        /// Verifies the shared client hello and authoritative server response frame contract.
        /// </summary>
        [Test]
        public void HandshakeFrames_RoundTripServerDecision()
        {
            var clientProtocol = new NetworkProtocolDescriptor("game", 1, 2, 1, "client-schema");
            var serverProtocol = new NetworkProtocolDescriptor("game", 1, 3, 2, "server-schema");
            var hello = NetworkProtocolHandshakeFrame.CreateHello(in clientProtocol);

            Assert.IsTrue(NetworkProtocolHandshakeFrame.IsFrame(hello));
            Assert.IsTrue(NetworkProtocolHandshakeFrame.TryReadHello(hello, out var restoredClient));
            Assert.AreEqual(clientProtocol, restoredClient);

            var responseFrame = NetworkProtocolHandshakeFrame.CreateResponse(in serverProtocol, in restoredClient);
            Assert.IsTrue(NetworkProtocolHandshakeFrame.TryReadResponse(responseFrame, out var response));
            Assert.AreEqual(serverProtocol, response.RemoteProtocol);
            Assert.AreEqual(ENetworkProtocolCompatibility.Compatible, response.Compatibility);
        }

        /// <summary>
        /// Verifies that automatic reconnection validates the replacement transport session again.
        /// </summary>
        [Test]
        public void Session_ReconnectRunsProtocolHandshakeAgain()
        {
            MessageSerializer serializer = new();
            TestTransport transport = new();
            var protocol = new NetworkProtocolDescriptor("game", 1, 2, 1, "schema-v2");
            transport.HandshakeProtocol = protocol;
            NetworkSessionOptions options = new();
            options.UseProtocolHandshake(in protocol).UseReconnect(2, 0.0);
            var application = new GameApplicationBuilder()
                .UseNetwork(serializer, transport, options)
                .Build();
            application.Start();
            var service = application.Services.GetRequired<INetworkService>();
            service.ConnectAsync("localhost", 9000).GetAwaiter().GetResult();

            transport.CurrentState = ENetworkState.Disconnected;
            FrameTime time = new(0.1, 0.1, 0.1, 0);
            application.Update(in time);

            Assert.AreEqual(2, transport.HandshakeCount);
            Assert.AreEqual(ENetworkState.Connected, service.State);
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

            /// <summary>
            /// Stores the number of client hello frames answered by the test peer.
            /// </summary>
            internal int HandshakeCount;

            /// <summary>
            /// Stores the authoritative protocol advertised by the test peer.
            /// </summary>
            internal NetworkProtocolDescriptor HandshakeProtocol;

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
                if (HandshakeProtocol.IsValid && NetworkProtocolHandshakeFrame.TryReadHello(frame, out var clientProtocol))
                {
                    HandshakeCount++;
                    Push(NetworkProtocolHandshakeFrame.CreateResponse(in HandshakeProtocol, in clientProtocol));
                }
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
