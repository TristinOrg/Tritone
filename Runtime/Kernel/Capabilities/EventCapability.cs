using System;
using Tritone.Events;

namespace Tritone.Kernel
{

    /// <summary>
    /// Provides event bindings whose ownership follows one module context.
    /// </summary>
    public sealed class EventCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        /// <summary>
        /// Initializes event operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal EventCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Binds one parameterless event for the module lifetime.
        /// </summary>
        /// <param name="eventSource">The event that owns the listener.</param>
        /// <param name="listener">The listener invoked when the event is published.</param>
        public void Bind(Event eventSource, Action listener)
        {
            mContext.Scope.Own(
                eventSource?.Bind(listener) ??
                throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds one single-value event for the module lifetime.
        /// </summary>
        /// <typeparam name="T1">The published value type.</typeparam>
        /// <param name="eventSource">The event that owns the listener.</param>
        /// <param name="listener">The listener invoked when the event is published.</param>
        public void Bind<T1>(Event<T1> eventSource, Action<T1> listener)
        {
            mContext.Scope.Own(
                eventSource?.Bind(listener) ??
                throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds one two-value event for the module lifetime.
        /// </summary>
        /// <typeparam name="T1">The first published value type.</typeparam>
        /// <typeparam name="T2">The second published value type.</typeparam>
        /// <param name="eventSource">The event that owns the listener.</param>
        /// <param name="listener">The listener invoked when the event is published.</param>
        public void Bind<T1, T2>(Event<T1, T2> eventSource, Action<T1, T2> listener)
        {
            mContext.Scope.Own(
                eventSource?.Bind(listener) ??
                throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds one three-value event for the module lifetime.
        /// </summary>
        /// <typeparam name="T1">The first published value type.</typeparam>
        /// <typeparam name="T2">The second published value type.</typeparam>
        /// <typeparam name="T3">The third published value type.</typeparam>
        /// <param name="eventSource">The event that owns the listener.</param>
        /// <param name="listener">The listener invoked when the event is published.</param>
        public void Bind<T1, T2, T3>(Event<T1, T2, T3> eventSource,
                                     Action<T1, T2, T3> listener)
        {
            mContext.Scope.Own(
                eventSource?.Bind(listener) ??
                throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds one four-value event for the module lifetime.
        /// </summary>
        /// <typeparam name="T1">The first published value type.</typeparam>
        /// <typeparam name="T2">The second published value type.</typeparam>
        /// <typeparam name="T3">The third published value type.</typeparam>
        /// <typeparam name="T4">The fourth published value type.</typeparam>
        /// <param name="eventSource">The event that owns the listener.</param>
        /// <param name="listener">The listener invoked when the event is published.</param>
        public void Bind<T1, T2, T3, T4>(Event<T1, T2, T3, T4> eventSource,
                                         Action<T1, T2, T3, T4> listener)
        {
            mContext.Scope.Own(
                eventSource?.Bind(listener) ??
                throw new ArgumentNullException(nameof(eventSource)));
        }
    }

}
