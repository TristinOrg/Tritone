using System;
using System.Collections.Generic;
using Tritone.Events;
using Tritone.Kernel;
using UnityEngine;

namespace Tritone.Unity
{
    /// <summary>
    /// Provides module access and automatically managed Tritone event bindings for Unity components.
    /// </summary>
    public abstract class TritoneComponent : MonoBehaviour
    {
        /// <summary>
        /// Stores bindings owned by this component.
        /// </summary>
        private List<IDisposable> mBindings;

        /// <summary>
        /// Gets one registered application module.
        /// </summary>
        /// <typeparam name="TModule">The concrete module type to resolve.</typeparam>
        /// <returns>The registered module instance.</returns>
        protected TModule GetModule<TModule>() where TModule : class
        {
            var application = TritoneBootstrap.Current;
            if (application == null)
                throw new InvalidOperationException("No running Tritone bootstrap is available.");

            return application.Services.GetRequired<TModule>();
        }

        /// <summary>Binds a parameterless Tritone event.</summary>
        /// <param name="eventSource">The event that owns the listener.</param>
        /// <param name="listener">The callback invoked when the event is published.</param>
        protected void BindEvent(Event eventSource, Action listener)
        {
            AddBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>Binds a one-parameter Tritone event.</summary>
        protected void BindEvent<T1>(Event<T1> eventSource, Action<T1> listener)
        {
            AddBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>Binds a two-parameter Tritone event.</summary>
        protected void BindEvent<T1, T2>(Event<T1, T2> eventSource, Action<T1, T2> listener)
        {
            AddBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>Binds a three-parameter Tritone event.</summary>
        protected void BindEvent<T1, T2, T3>(Event<T1, T2, T3> eventSource, Action<T1, T2, T3> listener)
        {
            AddBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>Binds a four-parameter Tritone event.</summary>
        protected void BindEvent<T1, T2, T3, T4>(Event<T1, T2, T3, T4> eventSource,
                                                  Action<T1, T2, T3, T4> listener)
        {
            AddBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Stores one disposable binding for automatic cleanup.
        /// </summary>
        /// <param name="binding">The binding owned by this component.</param>
        protected void AddBinding(IDisposable binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            mBindings ??= new();
            mBindings.Add(binding);
        }

        /// <summary>
        /// Releases every binding owned by this component in reverse registration order.
        /// </summary>
        protected void ReleaseBindings()
        {
            if (mBindings == null)
                return;

            for (int i = mBindings.Count - 1; i >= 0; i--)
                mBindings[i].Dispose();
            mBindings.Clear();
        }

        /// <summary>
        /// Releases retained listeners when Unity destroys this component.
        /// </summary>
        protected virtual void OnDestroy()
        {
            ReleaseBindings();
        }
    }
}
