using System;

namespace Tritone.Input
{
    /// <summary>
    /// Creates independently owned input binding lifetimes.
    /// </summary>
    public interface IInputService
    {
        /// <summary>
        /// Creates one empty input binding scope.
        /// </summary>
        IInputScope CreateScope();
    }

    /// <summary>
    /// Owns button and axis callbacks for one module lifetime.
    /// </summary>
    public interface IInputScope : IDisposable
    {
        /// <summary>
        /// Binds one button-down callback.
        /// </summary>
        void BindButton(string action, Action callback);

        /// <summary>
        /// Binds one axis callback that runs only after its value changes.
        /// </summary>
        void BindAxis(string action, Action<float> callback, float deadZone = 0.001f);
    }
}
