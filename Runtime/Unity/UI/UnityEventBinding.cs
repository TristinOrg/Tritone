using System;
using UnityEngine.Events;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Owns one parameterless Unity event listener registration.
    /// </summary>
    internal sealed class UnityEventBinding : IDisposable
    {
        private UnityEvent  mEventSource;
        private UnityAction mListener;

        internal UnityEventBinding(UnityEvent eventSource, UnityAction listener)
        {
            mEventSource = eventSource;
            mListener    = listener;
            mEventSource.AddListener(mListener);
        }

        public void Dispose()
        {
            if (mEventSource == null)
                return;

            mEventSource.RemoveListener(mListener);
            mEventSource = null;
            mListener    = null;
        }
    }

    /// <summary>
    /// Owns one strongly typed Unity event listener registration.
    /// </summary>
    /// <typeparam name="TValue">The Unity event payload type.</typeparam>
    internal sealed class UnityEventBinding<TValue> : IDisposable
    {
        private UnityEvent<TValue>  mEventSource;
        private UnityAction<TValue> mListener;

        internal UnityEventBinding(UnityEvent<TValue> eventSource, UnityAction<TValue> listener)
        {
            mEventSource = eventSource;
            mListener    = listener;
            mEventSource.AddListener(mListener);
        }

        public void Dispose()
        {
            if (mEventSource == null)
                return;

            mEventSource.RemoveListener(mListener);
            mEventSource = null;
            mListener    = null;
        }
    }
}
