using System;

namespace Tritone.Dispatching
{
    /// <summary>
    /// Stores one queued callback and its module ownership without another heap allocation.
    /// </summary>
    internal readonly struct DispatchEntry
    {
        /// <summary>
        /// Stores the unique scheduler identifier.
        /// </summary>
        internal readonly long Id;

        /// <summary>
        /// Stores the callback invoked on the application thread.
        /// </summary>
        internal readonly Action Callback;

        /// <summary>
        /// Stores the scope that owns and may cancel this callback.
        /// </summary>
        internal readonly MainThreadDispatchScope Owner;

        /// <summary>
        /// Initializes one immutable queued callback entry.
        /// </summary>
        /// <param name="id">The unique scheduler identifier.</param>
        /// <param name="callback">The callback invoked on the application thread.</param>
        /// <param name="owner">The scope that owns this callback.</param>
        internal DispatchEntry(long id, Action callback, MainThreadDispatchScope owner)
        {
            Id       = id;
            Callback = callback;
            Owner    = owner;
        }
    }
}
