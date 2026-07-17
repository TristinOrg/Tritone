using System;
using System.IO;
using System.Text;

namespace Tritone.Messaging
{
    /// <summary>
    /// Reads primitive message fields with strict frame boundary validation.
    /// </summary>
    public sealed class MessageReader
    {
        private readonly byte[] mBuffer;
        private int mPosition;

        public int Remaining => mBuffer.Length - mPosition;

        public MessageReader(byte[] buffer)
        {
            mBuffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        }

        public int ReadInt32()
        {
            Require(4);
            return mBuffer[mPosition++] |
                   mBuffer[mPosition++] << 8 |
                   mBuffer[mPosition++] << 16 |
                   mBuffer[mPosition++] << 24;
        }

        public float ReadSingle()
        {
            Require(4);
            var value = BitConverter.ToSingle(mBuffer, mPosition);
            mPosition += 4;
            return value;
        }

        public bool ReadBoolean()
        {
            Require(1);
            var value = mBuffer[mPosition++];
            if (value > 1)
                throw new InvalidDataException("A Boolean field must contain zero or one.");
            return value == 1;
        }

        public string ReadString()
        {
            var count = ReadLength();
            if (count < 0)
                return null;
            Require(count);
            var value = Encoding.UTF8.GetString(mBuffer, mPosition, count);
            mPosition += count;
            return value;
        }

        public byte[] ReadBytes()
        {
            var count = ReadLength();
            if (count < 0)
                return null;
            Require(count);
            var value = new byte[count];
            Buffer.BlockCopy(mBuffer, mPosition, value, 0, count);
            mPosition += count;
            return value;
        }

        private int ReadLength()
        {
            var value = ReadInt32();
            if (value < -1)
                throw new InvalidDataException("A field length cannot be less than minus one.");
            return value;
        }

        private void Require(int count)
        {
            if (count < 0 || count > Remaining)
                throw new EndOfStreamException("The message frame ended before the field was complete.");
        }
    }
}
