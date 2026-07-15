using UnityEngine;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Provides non-generic window operations required by the UI module.
    /// </summary>
    public interface IUIWindow
    {
        /// <summary>Gets the GameObject that owns the window.</summary>
        GameObject GameObject { get; }

        /// <summary>Activates the window.</summary>
        void Open();

        /// <summary>Deactivates the window.</summary>
        void Close();
    }
}
