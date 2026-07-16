using System;

namespace Tritone.UI
{
    /// <summary>
    /// Provides application-wide window access without exposing Unity implementation details.
    /// </summary>
    public interface IUIService
    {
        /// <summary>
        /// Creates one module-owned window availability scope.
        /// </summary>
        /// <returns>A new window scope.</returns>
        IUIWindowScope CreateScope();

        /// <summary>
        /// Opens one currently available window.
        /// </summary>
        /// <param name="windowType">The concrete window type.</param>
        /// <returns>The opened window instance.</returns>
        object OpenWindow(Type windowType);

        /// <summary>
        /// Closes one created window.
        /// </summary>
        /// <param name="windowType">The concrete window type.</param>
        /// <returns>True when a created window was closed; otherwise, false.</returns>
        bool CloseWindow(Type windowType);

        /// <summary>
        /// Gets one previously created window.
        /// </summary>
        /// <param name="windowType">The concrete window type.</param>
        /// <returns>The window instance, or null when it has not been created.</returns>
        object GetWindow(Type windowType);

        /// <summary>
        /// Determines whether one window is currently open.
        /// </summary>
        /// <param name="windowType">The concrete window type.</param>
        /// <returns>True when the window is active; otherwise, false.</returns>
        bool IsWindowOpen(Type windowType);
    }
}
