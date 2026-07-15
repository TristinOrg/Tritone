using System;
using System.Collections.Generic;
using Tritone.Events;
using Tritone.Kernel;
using UnityEngine;

namespace Tritone.Unity
{
    /// <summary>
    /// Provides automatic event binding cleanup and module access for Unity components.
    /// </summary>
    public abstract class TritoneBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Stores event bindings owned by the current enabled lifetime.
        /// </summary>
        private List<EventBinding> mEventBindings;

        /// <summary>
        /// Binds events whenever this component becomes enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            OnBindEvents();
        }

        /// <summary>
        /// Releases every event binding whenever this component becomes disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            UnbindAllEvents();
        }

        /// <summary>
        /// Defines the event bindings required while this component is enabled.
        /// </summary>
        protected virtual void OnBindEvents() { }

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

        /// <summary>
        /// Binds a parameterless event for the current enabled lifetime.
        /// </summary>
        protected void BindEvent(Event eventSource, Action listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a one-parameter event for the current enabled lifetime.
        /// </summary>
        protected void BindEvent<T1>(Event<T1> eventSource, Action<T1> listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a two-parameter event for the current enabled lifetime.
        /// </summary>
        protected void BindEvent<T1, T2>(Event<T1, T2> eventSource, Action<T1, T2> listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a three-parameter event for the current enabled lifetime.
        /// </summary>
        protected void BindEvent<T1, T2, T3>(Event<T1, T2, T3> eventSource, Action<T1, T2, T3> listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a four-parameter event for the current enabled lifetime.
        /// </summary>
        protected void BindEvent<T1, T2, T3, T4>(Event<T1, T2, T3, T4> eventSource,
                                                  Action<T1, T2, T3, T4> listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Stores one binding for automatic enabled-lifetime cleanup.
        /// </summary>
        /// <param name="binding">The newly created event binding.</param>
        private void AddEventBinding(EventBinding binding)
        {
            mEventBindings ??= new();
            mEventBindings.Add(binding);
        }

        /// <summary>
        /// Releases all event bindings in reverse registration order.
        /// </summary>
        private void UnbindAllEvents()
        {
            if (mEventBindings == null)
                return;

            for (int i = mEventBindings.Count - 1; i >= 0; i--)
                mEventBindings[i].Dispose();
            mEventBindings.Clear();
        }
    }
}
