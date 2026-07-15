using System;
using Tritone.UI;
using UnityEngine;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Stores fixed prefab, layer, and lifetime configuration for all known windows.
    /// </summary>
    [CreateAssetMenu(fileName = "UIWindowCatalog", menuName = "Tritone/UI/Window Catalog")]
    public sealed class UIWindowCatalog : ScriptableObject
    {
        /// <summary>
        /// Stores all window definitions configured by the project.
        /// </summary>
        public UIWindowDefinition[] Windows = Array.Empty<UIWindowDefinition>();
    }

    /// <summary>
    /// Defines one window prefab and its fixed runtime configuration.
    /// </summary>
    [Serializable]
    public sealed class UIWindowDefinition
    {
        /// <summary>The prefab containing one UIWindow component.</summary>
        public GameObject Prefab;

        /// <summary>The visual layer that receives created instances.</summary>
        public EUILayer Layer = EUILayer.Normal;

        /// <summary>The lifetime that controls window availability.</summary>
        public EUIWindowLifetime Lifetime = EUIWindowLifetime.Module;
    }
}
