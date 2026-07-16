using System;
using Tritone.UI;
using UnityEngine;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Stores the scene transforms used as window layer parents.
    /// </summary>
    public sealed class UIRoot : MonoBehaviour
    {
        // Stores the background layer parent.
        public RectTransform Background;

        // Stores the normal layer parent.
        public RectTransform Normal;

        // Stores the popup layer parent.
        public RectTransform Popup;

        // Stores the guide layer parent.
        public RectTransform Guide;

        // Stores the loading layer parent.
        public RectTransform Loading;

        // Stores the system layer parent.
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
