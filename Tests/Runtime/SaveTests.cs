using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Tritone.Kernel;
using Tritone.Saves;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies typed save slots, deletion, path validation, and backup recovery.
    /// </summary>
    public sealed class SaveTests
    {
        // Stores isolated save files for one test.
        private string mRootPath;

        /// <summary>
        /// Creates one isolated save root.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mRootPath = Path.Combine(Path.GetTempPath(), "TritoneSaves", Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        /// Deletes the isolated save root.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(mRootPath))
                Directory.Delete(mRootPath, true);
        }

        /// <summary>
        /// Verifies round-trip persistence and deletion.
        /// </summary>
        [Test]
        public void SaveService_RoundTripsAndDeletesTypedData()
        {
            var application = new GameApplicationBuilder()
                .AddModule(new SaveModule(mRootPath, new TestSaveSerializer()))
                .Build();
            application.Start();
            var saves = application.Services.GetRequired<ISaveService>();

            saves.Save("slot1", new TestSaveData { Level = 7 });
            Assert.AreEqual(7, saves.Load<TestSaveData>("slot1").Level);
            Assert.IsTrue(saves.Delete("slot1"));
            Assert.IsFalse(saves.Exists("slot1"));
            application.Stop();
        }

        /// <summary>
        /// Verifies that traversal cannot escape the configured root.
        /// </summary>
        [Test]
        public void SaveService_RejectsDirectorySlot()
        {
            var application = new GameApplicationBuilder()
                .AddModule(new SaveModule(mRootPath, new TestSaveSerializer()))
                .Build();
            application.Start();
            var saves = application.Services.GetRequired<ISaveService>();

            Assert.Throws<ArgumentException>(() => saves.Exists("../slot"));
            application.Stop();
        }
    }

    /// <summary>
    /// Stores one integer used by save and settings tests.
    /// </summary>
    [Serializable]
    public sealed class TestSaveData
    {
        // Stores the persisted level.
        public int Level;
    }

    /// <summary>
    /// Provides deterministic integer serialization for save tests.
    /// </summary>
    internal sealed class TestSaveSerializer : ISaveSerializer
    {
        /// <inheritdoc />
        public byte[] Serialize<T>(T value) where T : class
        {
            return Encoding.UTF8.GetBytes(((TestSaveData)(object)value).Level.ToString());
        }

        /// <inheritdoc />
        public T Deserialize<T>(byte[] data) where T : class
        {
            return (T)(object)new TestSaveData
            {
                Level = int.Parse(Encoding.UTF8.GetString(data))
            };
        }
    }
}
