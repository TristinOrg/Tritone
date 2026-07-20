using System;

namespace Tritone.Diagnostics
{
    /// <summary>
    /// Stores recent log events in a fixed-capacity thread-safe ring buffer.
    /// </summary>
    public sealed class RuntimeLogBufferSink : ILogSink
    {
        /// <summary>
        /// Synchronizes writes and indexed reads.
        /// </summary>
        private readonly object mSyncRoot = new();

        /// <summary>
        /// Stores log events in ring order.
        /// </summary>
        private readonly LogEvent[] mEvents;

        /// <summary>
        /// Stores the oldest event index.
        /// </summary>
        private int mStartIndex;

        /// <summary>
        /// Initializes one fixed-capacity log buffer.
        /// </summary>
        /// <param name="capacity">The positive number of retained events.</param>
        public RuntimeLogBufferSink(int capacity = 128)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            mEvents = new LogEvent[capacity];
        }

        /// <summary>
        /// Gets the maximum number of retained events.
        /// </summary>
        public int Capacity => mEvents.Length;

        /// <summary>
        /// Gets the current number of retained events.
        /// </summary>
        public int Count { get; private set; }

        /// <inheritdoc />
        public void Write(in LogEvent logEvent)
        {
            lock (mSyncRoot)
            {
                var index = (mStartIndex + Count) % mEvents.Length;
                if (Count == mEvents.Length)
                {
                    index = mStartIndex;
                    mStartIndex = (mStartIndex + 1) % mEvents.Length;
                }
                else
                    Count++;

                mEvents[index] = logEvent;
            }
        }

        /// <summary>
        /// Gets one retained event in oldest-to-newest order.
        /// </summary>
        /// <param name="index">The zero-based chronological index.</param>
        /// <returns>The retained log event.</returns>
        public LogEvent GetAt(int index)
        {
            lock (mSyncRoot)
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return mEvents[(mStartIndex + index) % mEvents.Length];
            }
        }

        /// <summary>
        /// Clears retained events without reallocating storage.
        /// </summary>
        public void Clear()
        {
            lock (mSyncRoot)
            {
                Array.Clear(mEvents, 0, mEvents.Length);
                mStartIndex = 0;
                Count       = 0;
            }
        }
    }
}
