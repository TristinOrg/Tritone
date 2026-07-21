using System;
using System.IO;
using NUnit.Framework;
using Tritone.Editor.CodeGeneration;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies deterministic table and network source generation.
    /// </summary>
    public sealed class CodeGenerationTests
    {
        // Stores the isolated generation directory used by each test.
        private string mOutputPath;

        [SetUp]
        public void SetUp()
        {
            mOutputPath = Path.Combine("Temp", "TritoneGenerationTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(mOutputPath))
                Directory.Delete(mOutputPath, true);
        }

        [Test]
        public void Tables_GenerateIncrementally()
        {
            TableSchema schema = new()
            {
                Namespace  = "Game.Tables",
                OutputPath = mOutputPath,
                Tables     = new[]
                {
                    new TableDefinition
                    {
                        Name   = "Role",
                        Path   = "Tables/Roles",
                        Fields = new[]
                        {
                            new TableFieldDefinition
                            {
                                Name = "Id",
                                Type = "int",
                                Key  = true
                            },
                            new TableFieldDefinition
                            {
                                Name = "Name",
                                Type = "string"
                            }
                        }
                    }
                }
            };

            Assert.IsTrue(TableCodeGenerator.Generate(schema));
            Assert.IsFalse(TableCodeGenerator.Generate(schema));
            var source = File.ReadAllText(
                Path.Combine(mOutputPath, "RoleTable.Generated.cs"));
            StringAssert.Contains("ITableRow<int>", source);
            StringAssert.Contains("public int Key => Id;", source);
        }

        [Test]
        public void Network_GeneratesRelationshipsCodecsAndRegistry()
        {
            NetworkSchema schema = CreateNetworkSchema();

            Assert.IsTrue(NetworkCodeGenerator.Generate(schema));
            Assert.IsFalse(NetworkCodeGenerator.Generate(schema));
            var source = File.ReadAllText(
                Path.Combine(mOutputPath, "LoginRequestMessage.Generated.cs"));
            StringAssert.Contains("INetworkRequest<LoginResponse>", source);
            StringAssert.Contains("writer.WriteInt32(message.RequestId);", source);
            var registry = File.ReadAllText(
                Path.Combine(mOutputPath, "NetworkMessages.Generated.cs"));
            StringAssert.Contains("LoginRequestId = 1001", registry);
            StringAssert.Contains("serializer.Register", registry);
            StringAssert.Contains("NetworkProtocolDescriptor Protocol", registry);
            StringAssert.Contains("\"game-main\", 2, 3, 1", registry);
        }

        private NetworkSchema CreateNetworkSchema()
        {
            return new NetworkSchema
            {
                ProtocolId          = "game-main",
                MajorVersion        = 2,
                MinorVersion        = 3,
                MinimumMinorVersion = 1,
                Namespace           = "Game.Network",
                OutputPath          = mOutputPath,
                Messages            = new[]
                {
                    new NetworkMessageDefinition
                    {
                        Id       = 1001,
                        Name     = "LoginRequest",
                        Kind     = "Request",
                        Response = "LoginResponse",
                        Fields   = new[]
                        {
                            new NetworkFieldDefinition
                            {
                                Name = "Account",
                                Type = "string"
                            }
                        }
                    },
                    new NetworkMessageDefinition
                    {
                        Id     = 1002,
                        Name   = "LoginResponse",
                        Kind   = "Response",
                        Fields = Array.Empty<NetworkFieldDefinition>()
                    }
                }
            };
        }
    }
}
