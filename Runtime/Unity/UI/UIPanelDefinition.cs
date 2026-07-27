using UnityEngine;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Stores one window-owned panel registration and its cached runtime state.
    /// </summary>
    internal sealed class UIPanelDefinition
    {
        /// <summary>
        /// Stores the registered asset path.
        /// </summary>
        internal readonly string AssetPath;

        /// <summary>
        /// Stores the default parent used when opening the panel.
        /// </summary>
        internal readonly Transform Parent;

        /// <summary>
        /// Stores the loaded panel prefab component.
        /// </summary>
        internal Component Prefab;

        /// <summary>
        /// Stores the cached panel behavior.
        /// </summary>
        internal IUIPanel Instance;

        /// <summary>
        /// Stores the cached panel GameObject for Unity-safe null checks.
        /// </summary>
        internal GameObject InstanceObject;

        /// <summary>
        /// Creates one immutable panel registration.
        /// </summary>
        /// <param name="assetPath">The provider path used to load the panel prefab.</param>
        /// <param name="parent">The default panel parent.</param>
        internal UIPanelDefinition(string assetPath, Transform parent)
        {
            AssetPath = assetPath;
            Parent    = parent;
        }
    }
}
