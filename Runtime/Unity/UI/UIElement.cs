using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Provides a typed view and an independent event-binding lifecycle for one UI object.
    /// </summary>
    /// <typeparam name="TView">The view component attached to the same prefab.</typeparam>
    public abstract class UIElement<TView> : TritoneComponent where TView : UIView
    {
        /// <summary>
        /// Stores the serialized view component used by the UI logic class.
        /// </summary>
        protected TView mView;

        /// <summary>
        /// Resolves the typed view and performs one-time initialization.
        /// </summary>
        protected virtual void Awake()
        {
            mView = GetComponent<TView>();
            if (mView == null)
                throw new InvalidOperationException($"{GetType().Name} requires {typeof(TView).Name} on the same GameObject.");

            OnInitialize();
        }

        /// <summary>
        /// Runs the dedicated binding stage before the UI object opens.
        /// </summary>
        protected virtual void OnEnable()
        {
            try
            {
                OnBindEvents();
                OnOpen();
            }
            catch
            {
                ReleaseBindings();
                throw;
            }
        }

        /// <summary>
        /// Runs close logic and releases all Unity and Tritone event listeners.
        /// </summary>
        protected virtual void OnDisable()
        {
            try
            {
                OnClose();
            }
            finally
            {
                ReleaseBindings();
            }
        }

        /// <summary>
        /// Performs one-time non-binding initialization after the typed view is resolved.
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// Binds all Unity control and Tritone event listeners for the enabled lifetime.
        /// </summary>
        protected virtual void OnBindEvents() { }

        /// <summary>
        /// Refreshes UI state whenever the object becomes enabled.
        /// </summary>
        protected virtual void OnOpen() { }

        /// <summary>
        /// Handles UI state whenever the object becomes disabled.
        /// </summary>
        protected virtual void OnClose() { }

        /// <summary>
        /// Releases non-event resources before the UI object is destroyed.
        /// </summary>
        protected virtual void OnRelease() { }

        /// <summary>Binds a button click listener for the enabled lifetime.</summary>
        protected void BindButton(Button button, UnityAction listener)
        {
            if (button == null)
                throw new ArgumentNullException(nameof(button));
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            AddBinding(new UnityEventBinding(button.onClick, listener));
        }

        /// <summary>Binds a toggle value listener for the enabled lifetime.</summary>
        protected void BindToggle(Toggle toggle, UnityAction<bool> listener)
        {
            if (toggle == null)
                throw new ArgumentNullException(nameof(toggle));
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            AddBinding(new UnityEventBinding<bool>(toggle.onValueChanged, listener));
        }

        /// <summary>Binds a slider value listener for the enabled lifetime.</summary>
        protected void BindSlider(Slider slider, UnityAction<float> listener)
        {
            if (slider == null)
                throw new ArgumentNullException(nameof(slider));
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            AddBinding(new UnityEventBinding<float>(slider.onValueChanged, listener));
        }

        /// <summary>Binds a dropdown value listener for the enabled lifetime.</summary>
        protected void BindDropdown(Dropdown dropdown, UnityAction<int> listener)
        {
            if (dropdown == null)
                throw new ArgumentNullException(nameof(dropdown));
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            AddBinding(new UnityEventBinding<int>(dropdown.onValueChanged, listener));
        }

        /// <summary>Binds an input value listener for the enabled lifetime.</summary>
        protected void BindInputValue(InputField input, UnityAction<string> listener)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            AddBinding(new UnityEventBinding<string>(input.onValueChanged, listener));
        }

        /// <summary>Binds an input submission listener for the enabled lifetime.</summary>
        protected void BindInputEndEdit(InputField input, UnityAction<string> listener)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            AddBinding(new UnityEventBinding<string>(input.onEndEdit, listener));
        }

        /// <summary>
        /// Runs final UI cleanup and then releases common component resources.
        /// </summary>
        protected override void OnDestroy()
        {
            try
            {
                OnRelease();
            }
            finally
            {
                base.OnDestroy();
            }
        }
    }
}
