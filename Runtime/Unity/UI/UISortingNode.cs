using System;
using UnityEngine;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Stores one preprocessed UI render node and its authoring hierarchy metadata.
    /// </summary>
    [Serializable]
    public struct UISortingNode
    {
        /// <summary>
        /// Stores the Canvas or Renderer whose sorting order is controlled at runtime.
        /// </summary>
        public Component Target;

        /// <summary>
        /// Stores the normalized visual order captured during preprocessing.
        /// </summary>
        public int RelativeOrder;

        /// <summary>
        /// Stores the transform depth relative to the view root.
        /// </summary>
        public int HierarchyDepth;
    }
}
