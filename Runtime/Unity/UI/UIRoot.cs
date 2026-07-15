using System;
using UnityEngine;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Stores the scene transforms used as window layer parents.
    /// </summary>
    public sealed class UIRoot : MonoBehaviour
    {
        /// <summary>Stores the background layer parent.</summary>
        public RectTransform Background;

        /// <summary>Stores the normal layer parent.</summary>
        public RectTransform Normal;

        /// <summary>Stores the popup layer parent.</summary>
        public RectTransform Popup;

        /// <summary>Stores the guide layer parent.</summary>
        public RectTransform Guide;

        /// <summary>Stores the loading layer parent.</summary>
        public RectTransform Loading;

        /// <summary>Stores the system layer parent.</summary>
        public RectTransform System;

        /// <summary>
        /// Gets the transform assigned to one UI layer.
        /// </summary>
        /// <param name="layer">The requested UI layer.</param>
        /// <returns>The configured layer transform.</returns>
        public RectTransform GetLayer(EUILayer layer)
        {
            RectTransform root;
            switch (layer)
            {
                case EUILayer.Background: root = Background; break;
                case EUILayer.Normal:     root = Normal;     break;
                case EUILayer.Popup:      root = Popup;      break;
                case EUILayer.Guide:      root = Guide;      break;
                case EUILayer.Loading:    root = Loading;    break;
                case EUILayer.System:     root = System;     break;
                default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
            }

            if (root == null)
                throw new InvalidOperationException($"UI layer {layer} has no configured root.");
            return root;
        }
    }
}
