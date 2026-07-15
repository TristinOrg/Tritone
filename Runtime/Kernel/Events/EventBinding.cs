using System;

namespace Tritone.Events
{
    /// <summary>
    /// Represents one event listener registration that can be released exactly once.
    /// </summary>
    public sealed class EventBinding : IDisposable
    {
        /// <summary>
        /// Stores the event that owns the listener.
        /// </summary>
        private IEventSource mSource;

        /// <summary>
        /// Stores the exact listener registered with the event.
        /// </summary>
        private Delegate mListener;

        /// <summary>
        /// Initializes one event listener registration.
        /// </summary>
        /// <param name="source">The event that owns the listener.</param>
        /// <param name="listener">The listener registered with the event.</param>
        internal EventBinding(IEventSource source, Delegate listener)
        {
            mSource   = source;
            mListener = listener;
        }

        /// <summary>
        /// Removes the listener and releases retained references.
        /// </summary>
        public void Dispose()
        {
            if (mSource == null)
                return;

            mSource.Unbind(mListener);
            mSource   = null;
            mListener = null;
        }
    }

    /// <summary>
    /// Provides the non-generic unbind operation used by automatic lifetime management.
    /// </summary>
    internal interface IEventSource
    {
        /// <summary>
        /// Removes one previously registered listener.
        /// </summary>
        /// <param name="listener">The exact listener to remove.</param>
        void Unbind(Delegate listener);
    }
}
