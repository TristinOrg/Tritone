using System;

namespace Tritone.Events
{
    /// <summary>
    /// Provides a strongly typed event without payload parameters.
    /// </summary>
    public sealed class Event : IEventSource
    {
        private Action mListeners;

        /// <summary>
        /// Registers one listener.
        /// </summary>
        /// <param name="listener">The callback invoked when the event is published.</param>
        /// <returns>The binding used by a lifecycle owner to release the listener.</returns>
        public EventBinding Bind(Action listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            mListeners += listener;
            return new(this, listener);
        }

        /// <summary>
        /// Immediately invokes all currently registered listeners.
        /// </summary>
        public void Publish()
        {
            mListeners?.Invoke();
        }

        /// <summary>
        /// Removes every listener retained by this event.
        /// </summary>
        public void Clear()
        {
            mListeners = null;
        }

        void IEventSource.Unbind(Delegate listener)
        {
            mListeners -= (Action)listener;
        }
    }

    /// <summary>
    /// Provides a strongly typed event with one payload parameter.
    /// </summary>
    /// <typeparam name="T1">The first payload type.</typeparam>
    public sealed class Event<T1> : IEventSource
    {
        /// <summary>Stores all registered listeners.</summary>
        private Action<T1> mListeners;

        /// <summary>Registers one strongly typed listener.</summary>
        /// <param name="listener">The callback invoked when published.</param>
        /// <returns>The removable listener binding.</returns>
        public EventBinding Bind(Action<T1> listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));
            mListeners += listener;
            return new(this, listener);
        }

        /// <summary>Immediately invokes all listeners with one value.</summary>
        /// <param name="value1">The first payload value.</param>
        public void Publish(T1 value1)
        {
            mListeners?.Invoke(value1);
        }

        /// <summary>Removes every listener retained by this event.</summary>
        public void Clear()
        {
            mListeners = null;
        }

        void IEventSource.Unbind(Delegate listener)
        {
            mListeners -= (Action<T1>)listener;
        }
    }

    /// <summary>
    /// Provides a strongly typed event with two payload parameters.
    /// </summary>
    /// <typeparam name="T1">The first payload type.</typeparam>
    /// <typeparam name="T2">The second payload type.</typeparam>
    public sealed class Event<T1, T2> : IEventSource
    {
        /// <summary>Stores all registered listeners.</summary>
        private Action<T1, T2> mListeners;

        /// <summary>Registers one strongly typed listener.</summary>
        /// <param name="listener">The callback invoked when published.</param>
        /// <returns>The removable listener binding.</returns>
        public EventBinding Bind(Action<T1, T2> listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));
            mListeners += listener;
            return new(this, listener);
        }

        /// <summary>Immediately invokes all listeners with two values.</summary>
        /// <param name="value1">The first payload value.</param>
        /// <param name="value2">The second payload value.</param>
        public void Publish(T1 value1, T2 value2)
        {
            mListeners?.Invoke(value1, value2);
        }

        /// <summary>Removes every listener retained by this event.</summary>
        public void Clear()
        {
            mListeners = null;
        }

        void IEventSource.Unbind(Delegate listener)
        {
            mListeners -= (Action<T1, T2>)listener;
        }
    }

    /// <summary>
    /// Provides a strongly typed event with three payload parameters.
    /// </summary>
    /// <typeparam name="T1">The first payload type.</typeparam>
    /// <typeparam name="T2">The second payload type.</typeparam>
    /// <typeparam name="T3">The third payload type.</typeparam>
    public sealed class Event<T1, T2, T3> : IEventSource
    {
        /// <summary>Stores all registered listeners.</summary>
        private Action<T1, T2, T3> mListeners;

        /// <summary>Registers one strongly typed listener.</summary>
        /// <param name="listener">The callback invoked when published.</param>
        /// <returns>The removable listener binding.</returns>
        public EventBinding Bind(Action<T1, T2, T3> listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));
            mListeners += listener;
            return new(this, listener);
        }

        /// <summary>Immediately invokes all listeners with three values.</summary>
        /// <param name="value1">The first payload value.</param>
        /// <param name="value2">The second payload value.</param>
        /// <param name="value3">The third payload value.</param>
        public void Publish(T1 value1, T2 value2, T3 value3)
        {
            mListeners?.Invoke(value1, value2, value3);
        }

        /// <summary>Removes every listener retained by this event.</summary>
        public void Clear()
        {
            mListeners = null;
        }

        void IEventSource.Unbind(Delegate listener)
        {
            mListeners -= (Action<T1, T2, T3>)listener;
        }
    }

    /// <summary>
    /// Provides a strongly typed event with four payload parameters.
    /// </summary>
    /// <typeparam name="T1">The first payload type.</typeparam>
    /// <typeparam name="T2">The second payload type.</typeparam>
    /// <typeparam name="T3">The third payload type.</typeparam>
    /// <typeparam name="T4">The fourth payload type.</typeparam>
    public sealed class Event<T1, T2, T3, T4> : IEventSource
    {
        /// <summary>Stores all registered listeners.</summary>
        private Action<T1, T2, T3, T4> mListeners;

        /// <summary>Registers one strongly typed listener.</summary>
        /// <param name="listener">The callback invoked when published.</param>
        /// <returns>The removable listener binding.</returns>
        public EventBinding Bind(Action<T1, T2, T3, T4> listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));
            mListeners += listener;
            return new(this, listener);
        }

        /// <summary>Immediately invokes all listeners with four values.</summary>
        /// <param name="value1">The first payload value.</param>
        /// <param name="value2">The second payload value.</param>
        /// <param name="value3">The third payload value.</param>
        /// <param name="value4">The fourth payload value.</param>
        public void Publish(T1 value1, T2 value2, T3 value3, T4 value4)
        {
            mListeners?.Invoke(value1, value2, value3, value4);
        }

        /// <summary>Removes every listener retained by this event.</summary>
        public void Clear()
        {
            mListeners = null;
        }

        void IEventSource.Unbind(Delegate listener)
        {
            mListeners -= (Action<T1, T2, T3, T4>)listener;
        }
    }
}
