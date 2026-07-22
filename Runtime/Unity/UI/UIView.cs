using System;
using UnityEngine;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Marks a prefab component that contains serialized UI element references only.
    /// </summary>
    public abstract class UIView : MonoBehaviour
    {
        /// <summary>
        /// Stores the preprocessed render nodes in deterministic visual order.
        /// </summary>
        public UISortingNode[] SortingNodes;

        /// <summary>
        /// Applies consecutive sorting orders to every preprocessed render node.
        /// </summary>
        /// <param name="order">The next sorting order available to this view.</param>
        internal void ApplySortingOrder(ref int order)
        {
            if (SortingNodes == null)
                return;

            var firstOrder = order;
            foreach (var node in SortingNodes)
            {
                var target = node.Target;
                if (!target)
                    continue;
                var nodeOrder = firstOrder + node.RelativeOrder;
                if (target is Canvas canvas)
                {
                    canvas.overrideSorting = true;
                    canvas.sortingOrder    = nodeOrder;
                    order                  = Math.Max(order, nodeOrder + 1);
                    continue;
                }
                if (target is Renderer renderer)
                {
                    renderer.sortingOrder = nodeOrder;
                    order                 = Math.Max(order, nodeOrder + 1);
                }
            }
        }
    }
}
