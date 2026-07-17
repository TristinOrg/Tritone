using System;
using System.Collections.Generic;
using Tritone.Input;

namespace Tritone.Unity.Input
{
    /// <summary>
    /// Owns input bindings for one consumer lifetime.
    /// </summary>
    internal sealed class InputScope : IInputScope
    {
        private readonly InputModule mModule;
        private readonly List<InputBinding> mBindings = new();
        private bool mDisposed;

        internal InputScope(InputModule module)
        {
            mModule = module;
        }

        /// <inheritdoc />
        public void BindButton(string action, Action callback)
        {
            Validate(action, callback);
            Add(new InputBinding(action, callback));
        }

        /// <inheritdoc />
        public void BindAxis(string action, Action<float> callback, float deadZone = 0.001f)
        {
            Validate(action, callback);
            if (deadZone < 0.0f || deadZone > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(deadZone));
            Add(new InputBinding(action, callback, deadZone));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (mDisposed)
                return;
            mDisposed = true;
            for (int i = mBindings.Count - 1; i >= 0; i--)
                mModule.Remove(mBindings[i]);
            mBindings.Clear();
        }

        private void Add(InputBinding binding)
        {
            if (mDisposed)
                throw new ObjectDisposedException(nameof(InputScope));
            mBindings.Add(binding);
            mModule.Add(binding);
        }

        private static void Validate(string action, Delegate callback)
        {
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("An input action cannot be empty.", nameof(action));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
        }
    }
}
