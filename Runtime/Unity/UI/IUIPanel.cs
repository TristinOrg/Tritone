using UnityEngine;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Provides common operations for a window-owned UI panel.
    /// </summary>
    public interface IUIPanel
    {
        /// <summary>
        /// Gets the panel GameObject.
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// Gets the strongly bound panel view through its common base type.
        /// </summary>
        UIView View { get; }

        /// <summary>
        /// Opens the panel.
        /// </summary>
        void Open();

        /// <summary>
        /// Closes the panel.
        /// </summary>
        void Close();
    }
}
