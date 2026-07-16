using System;

namespace Tritone.UI
{
    /// <summary>
    /// Tracks the windows made available by one module and releases them as a group.
    /// </summary>
    public interface IUIWindowScope : IDisposable
    {
        /// <summary>
        /// Registers and owns one hot-update-friendly window definition.
        /// </summary>
        /// <param name="windowType">The concrete window component type.</param>
        /// <param name="assetPath">The provider path used to load the window prefab.</param>
        /// <param name="layer">The visual layer that receives the window.</param>
        /// <param name="lifetime">The lifetime controlling availability and release.</param>
        void AddWindow(Type windowType,
                       string assetPath,
                       EUILayer layer,
                       EUIWindowLifetime lifetime);
    }
}
