using System;

namespace Tritone.UI
{
    /// <summary>
    /// Tracks the windows made available by one module and releases them as a group.
    /// </summary>
    public interface IUIWindowScope : IDisposable
    {
        /// <summary>
        /// Adds one window type to the owning module.
        /// </summary>
        /// <param name="windowType">The concrete window type to make available.</param>
        void AddWindow(Type windowType);
    }
}
