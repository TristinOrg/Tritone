using System;
using System.Collections.Generic;

namespace Tritone.Timing
{
    /// <summary>
    /// Stores timers in a binary minimum heap ordered by expiration time and identifier.
    /// </summary>
    internal sealed class TimerQueue
    {
        /// <summary>
        /// Stores timer data in heap order.
        /// </summary>
        private TimerEntry[] mEntries;

        /// <summary>
        /// Maps timer identifiers to heap indices for constant-time lookup.
        /// </summary>
        private readonly Dictionary<ulong, int> mIndices;

        /// <summary>
        /// Initializes an empty timer queue with preallocated storage.
        /// </summary>
        /// <param name="capacity">The initial number of timers that can be stored without resizing.</param>
        internal TimerQueue(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            mEntries = new TimerEntry[capacity];
            mIndices = new(capacity);
        }

        /// <summary>
        /// Gets the number of active timers in this queue.
        /// </summary>
        internal int Count { get; private set; }

        /// <summary>
        /// Adds one timer while preserving heap order.
        /// </summary>
        /// <param name="entry">The timer data to add.</param>
        internal void Add(in TimerEntry entry)
        {
            EnsureCapacity(Count + 1);

            var index       = Count++;
            mEntries[index] = entry;
            mIndices.Add(entry.Id, index);
            SiftUp(index);
        }

        /// <summary>
        /// Removes an active timer by identifier.
        /// </summary>
        /// <param name="id">The timer identifier to remove.</param>
        /// <param name="owner">The timer scope that must own the timer.</param>
        /// <returns>True when an active timer was removed; otherwise, false.</returns>
        internal bool Remove(ulong id, TimerScope owner)
        {
            if (!mIndices.TryGetValue(id, out var index))
                return false;
            if (!ReferenceEquals(mEntries[index].Owner, owner))
                return false;

            RemoveAt(index, out _);
            return true;
        }

        /// <summary>
        /// Determines whether a timer identifier exists in this queue.
        /// </summary>
        /// <param name="id">The timer identifier to query.</param>
        /// <param name="owner">The timer scope that must own the timer.</param>
        /// <returns>True when the timer exists; otherwise, false.</returns>
        internal bool Contains(ulong id, TimerScope owner)
        {
            return mIndices.TryGetValue(id, out var index) && ReferenceEquals(mEntries[index].Owner, owner);
        }

        /// <summary>
        /// Determines whether a timer identifier exists regardless of ownership.
        /// </summary>
        /// <param name="id">The timer identifier to query.</param>
        /// <returns>True when the timer exists; otherwise, false.</returns>
        internal bool Contains(ulong id)
        {
            return mIndices.ContainsKey(id);
        }

        /// <summary>
        /// Removes every timer owned by one timer scope.
        /// </summary>
        /// <param name="owner">The timer scope whose timers must be removed.</param>
        internal void RemoveOwner(TimerScope owner)
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(mEntries[i].Owner, owner))
                    RemoveAt(i, out _);
            }
        }

        /// <summary>
        /// Removes the earliest timer when it has reached its expiration time.
        /// </summary>
        /// <param name="now">The current queue clock in seconds.</param>
        /// <param name="entry">The removed timer data when one is due.</param>
        /// <returns>True when a due timer was removed; otherwise, false.</returns>
        internal bool TryPopDue(double now, out TimerEntry entry)
        {
            if (Count == 0 || mEntries[0].DueTime > now)
            {
                entry = default;
                return false;
            }

            RemoveAt(0, out entry);
            return true;
        }

        /// <summary>
        /// Determines whether the earliest timer has reached its expiration time.
        /// </summary>
        /// <param name="now">The current queue clock in seconds.</param>
        /// <returns>True when at least one timer is due; otherwise, false.</returns>
        internal bool HasDue(double now)
        {
            return Count > 0 && mEntries[0].DueTime <= now;
        }

        /// <summary>
        /// Removes all timers and releases their callback references.
        /// </summary>
        internal void Clear()
        {
            Array.Clear(mEntries, 0, Count);
            mIndices.Clear();
            Count = 0;
        }

        /// <summary>
        /// Removes one heap entry and repairs heap order.
        /// </summary>
        /// <param name="index">The heap index to remove.</param>
        /// <param name="removed">The timer data removed from the heap.</param>
        private void RemoveAt(int index, out TimerEntry removed)
        {
            var lastIndex = Count - 1;
            removed       = mEntries[index];
            mIndices.Remove(removed.Id);

            if (index == lastIndex)
            {
                mEntries[lastIndex] = default;
                Count--;
                return;
            }

            var moved          = mEntries[lastIndex];
            mEntries[index]     = moved;
            mEntries[lastIndex] = default;
            Count--;
            mIndices[moved.Id] = index;

            var parentIndex = (index - 1) >> 1;
            if (index > 0 && IsEarlier(in mEntries[index], in mEntries[parentIndex]))
                SiftUp(index);
            else
                SiftDown(index);
        }

        /// <summary>
        /// Moves one entry toward the root until heap order is restored.
        /// </summary>
        /// <param name="index">The initial heap index.</param>
        private void SiftUp(int index)
        {
            while (index > 0)
            {
                var parentIndex = (index - 1) >> 1;
                if (!IsEarlier(in mEntries[index], in mEntries[parentIndex]))
                    break;

                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        /// <summary>
        /// Moves one entry toward the leaves until heap order is restored.
        /// </summary>
        /// <param name="index">The initial heap index.</param>
        private void SiftDown(int index)
        {
            while (true)
            {
                var leftIndex = (index << 1) + 1;
                if (leftIndex >= Count)
                    return;

                var rightIndex   = leftIndex + 1;
                var earlierIndex = leftIndex;
                if (rightIndex < Count && IsEarlier(in mEntries[rightIndex], in mEntries[leftIndex]))
                    earlierIndex = rightIndex;
                if (!IsEarlier(in mEntries[earlierIndex], in mEntries[index]))
                    return;

                Swap(index, earlierIndex);
                index = earlierIndex;
            }
        }

        /// <summary>
        /// Exchanges two heap entries and updates their lookup indices.
        /// </summary>
        /// <param name="leftIndex">The first heap index.</param>
        /// <param name="rightIndex">The second heap index.</param>
        private void Swap(int leftIndex, int rightIndex)
        {
            var value            = mEntries[leftIndex];
            mEntries[leftIndex]  = mEntries[rightIndex];
            mEntries[rightIndex] = value;
            mIndices[mEntries[leftIndex].Id]  = leftIndex;
            mIndices[mEntries[rightIndex].Id] = rightIndex;
        }

        /// <summary>
        /// Ensures the heap can store the requested number of timers.
        /// </summary>
        /// <param name="requiredCapacity">The required timer capacity.</param>
        private void EnsureCapacity(int requiredCapacity)
        {
            if (requiredCapacity <= mEntries.Length)
                return;

            var newCapacity = mEntries.Length << 1;
            if (newCapacity < requiredCapacity)
                newCapacity = requiredCapacity;
            Array.Resize(ref mEntries, newCapacity);
        }

        /// <summary>
        /// Determines whether one timer must execute before another timer.
        /// </summary>
        /// <param name="left">The first timer.</param>
        /// <param name="right">The second timer.</param>
        /// <returns>True when the first timer has higher heap priority; otherwise, false.</returns>
        private static bool IsEarlier(in TimerEntry left, in TimerEntry right)
        {
            return left.DueTime < right.DueTime || left.DueTime.Equals(right.DueTime) && left.Id < right.Id;
        }
    }

    /// <summary>
    /// Stores all runtime data required by one scheduled timer.
    /// </summary>
    internal struct TimerEntry
    {
        /// <summary>
        /// Gets the caller-defined key associated with this timer.
        /// </summary>
        internal TimerKey Key;

        /// <summary>
        /// Gets the unique timer identifier.
        /// </summary>
        internal ulong Id;

        /// <summary>
        /// Gets the absolute queue time at which the timer expires.
        /// </summary>
        internal double DueTime;

        /// <summary>
        /// Gets the repeat interval, or zero for a one-shot timer.
        /// </summary>
        internal double Interval;

        /// <summary>
        /// Gets the callback invoked when the timer expires.
        /// </summary>
        internal Action Callback;

        /// <summary>
        /// Gets the module timer scope that owns this timer.
        /// </summary>
        internal TimerScope Owner;
    }
}
