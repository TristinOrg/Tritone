using System;
using System.Collections.Generic;
using Tritone.Input;
using Tritone.Kernel;

namespace Tritone.Unity.Input
{
    /// <summary>
    /// Polls named input actions and dispatches lifecycle-owned callbacks.
    /// </summary>
    public sealed class InputModule : ModuleBase, IInputService, IUpdateSystem
    {
        // Reads concrete input state.
        private readonly IInputSource mSource;

        // Stores active button bindings.
        private readonly List<InputBinding> mButtons = new();

        // Stores active axis bindings.
        private readonly List<InputBinding> mAxes = new();

        // Indicates whether this module has stopped.
        private bool mStopped;

        /// <inheritdoc />
        public int Order => -1000;

        /// <summary>
        /// Initializes input with Unity's built-in input source.
        /// </summary>
        public InputModule() : this(new UnityInputSource()) { }

        /// <summary>
        /// Initializes input with one replaceable source.
        /// </summary>
        public InputModule(IInputSource source)
        {
            mSource = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <inheritdoc />
        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton<IInputService>(this);
        }

        /// <inheritdoc />
        public IInputScope CreateScope()
        {
            if (mStopped)
                throw new ObjectDisposedException(nameof(InputModule));
            return new InputScope(this);
        }

        /// <inheritdoc />
        public void Update(in FrameTime time)
        {
            for (int i = 0, cnt = mButtons.Count; i < cnt; i++)
            {
                var binding = mButtons[i];
                if (mSource.GetButtonDown(binding.Action))
                    binding.ButtonCallback.Invoke();
            }
            for (int i = 0, cnt = mAxes.Count; i < cnt; i++)
            {
                var binding = mAxes[i];
                var value   = mSource.GetAxis(binding.Action);
                if (Math.Abs(value) < binding.DeadZone)
                    value = 0.0f;
                if (Math.Abs(value - binding.LastValue) < binding.DeadZone)
                    continue;
                binding.LastValue = value;
                binding.AxisCallback.Invoke(value);
            }
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            mStopped = true;
            mButtons.Clear();
            mAxes.Clear();
        }

        /// <summary>
        /// Adds one validated binding.
        /// </summary>
        internal void Add(InputBinding binding)
        {
            (binding.AxisCallback == null ? mButtons : mAxes).Add(binding);
        }

        /// <summary>
        /// Removes one exact binding.
        /// </summary>
        internal void Remove(InputBinding binding)
        {
            var bindings = binding.AxisCallback == null ? mButtons : mAxes;
            var index    = bindings.IndexOf(binding);
            if (index < 0)
                return;
            var last      = bindings.Count - 1;
            bindings[index] = bindings[last];
            bindings.RemoveAt(last);
        }
    }

    /// <summary>
    /// Stores one input action callback and axis state.
    /// </summary>
    internal sealed class InputBinding
    {
        internal readonly string Action;
        internal readonly Action ButtonCallback;
        internal readonly Action<float> AxisCallback;
        internal readonly float DeadZone;
        internal float LastValue;

        internal InputBinding(string action, Action callback)
        {
            Action         = action;
            ButtonCallback = callback;
        }

        internal InputBinding(string action, Action<float> callback, float deadZone)
        {
            Action       = action;
            AxisCallback = callback;
            DeadZone     = deadZone;
        }
    }
}
