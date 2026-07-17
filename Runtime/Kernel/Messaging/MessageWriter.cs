using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Tritone.Messaging
{
    /// <summary>
    /// Writes primitive message fields into one reusable contiguous buffer.
    /// </summary>
    public sealed class MessageWriter
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct SingleUnion
        {
            [FieldOffset(0)]
            internal float Single;

            [FieldOffset(0)]
            internal int Int32;
        }

        // Stores the reusable output buffer.
        private byte[] mBuffer;

        // Stores the next write position.
        private int mPosition;

        public int Length => mPosition;

        public MessageWriter(int capacity = 128)
        {
            if (capacity < 4)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            mBuffer = new byte[capacity];
        }

        public void Reset()
        {
            mPosition = 0;
        }

        public void WriteInt32(int value)
        {
            Ensure(4);
            mBuffer[mPosition++] = (byte)value;
            mBuffer[mPosition++] = (byte)(value >> 8);
            mBuffer[mPosition++] = (byte)(value >> 16);
            mBuffer[mPosition++] = (byte)(value >> 24);
        }

        public void WriteSingle(float value)
        {
            SingleUnion union = new()
            {
                Single = value
            };
            WriteInt32(union.Int32);
        }

        public void WriteBoolean(bool value)
        {
            Ensure(1);
            mBuffer[mPosition++] = value ? (byte)1 : (byte)0;
        }

        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            var count = Encoding.UTF8.GetByteCount(value);
            WriteInt32(count);
            Ensure(count);
            mPosition += Encoding.UTF8.GetBytes(value, 0, value.Length, mBuffer, mPosition);
        }

        public void WriteBytes(byte[] value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }
            WriteInt32(value.Length);
            WriteBytesRaw(value, 0, value.Length);
        }

        public byte[] ToArray()
        {
            var result = new byte[mPosition];
            Buffer.BlockCopy(mBuffer, 0, result, 0, mPosition);
            return result;
        }

        private void WriteBytesRaw(byte[] value, int offset, int count)
        {
            Ensure(count);
            Buffer.BlockCopy(value, offset, mBuffer, mPosition, count);
            mPosition += count;
        }

        private void Ensure(int count)
        {
            var required = mPosition + count;
            if (required <= mBuffer.Length)
                return;
            var capacity = mBuffer.Length * 2;
            while (capacity < required)
                capacity *= 2;
            Array.Resize(ref mBuffer, capacity);
        }
    }
}
