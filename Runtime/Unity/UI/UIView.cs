using System;
using System.Collections.Generic;
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
        /// Stores active dynamically composed child views in creation order.
        /// </summary>
        private List<UIView> mSubViews;

        /// <summary>
        /// Stores the first sorting order assigned by the owning UI layer.
        /// </summary>
        private int mFirstSortingOrder;

        /// <summary>
        /// Stores the next sorting order available for an appended child view.
        /// </summary>
        private int mNextSortingOrder;

        /// <summary>
        /// Stores the exclusive upper bound assigned by the owning UI layer.
        /// </summary>
        private int mSortingOrderLimit;

        /// <summary>
        /// Indicates whether the owning UI layer has assigned a sorting interval.
        /// </summary>
        private bool mHasSortingOrder;

        /// <summary>
        /// Applies consecutive sorting orders to every preprocessed render node.
        /// </summary>
        /// <param name="order">The next sorting order available to this view.</param>
        /// <param name="limit">The exclusive upper bound assigned by the owning UI layer.</param>
        internal void ApplySortingOrder(ref int order, int limit = int.MaxValue)
        {
            mFirstSortingOrder = order;
            mSortingOrderLimit = limit;
            mHasSortingOrder   = true;
            ApplyOwnSortingOrder(ref order);
            if (mSubViews == null)
            {
                mNextSortingOrder = order;
                return;
            }
            foreach (var subView in mSubViews)
            {
                if (subView && subView.gameObject.activeInHierarchy)
                {
                    subView.ApplySortingOrder(ref order, limit);
                }
            }
            mNextSortingOrder = order;
        }

        /// <summary>
        /// Adds one active dynamically composed child view and refreshes local sorting.
        /// </summary>
        /// <param name="subView">The child view to add.</param>
        internal void AddSubView(UIView subView)
        {
            if (!subView)
            {
                throw new ArgumentNullException(nameof(subView));
            }
            mSubViews ??= new List<UIView>();
            if (mSubViews.Contains(subView))
            {
                return;
            }
            mSubViews.Add(subView);
            if (mHasSortingOrder && subView.gameObject.activeInHierarchy)
            {
                var order = mNextSortingOrder;
                subView.ApplySortingOrder(ref order, mSortingOrderLimit);
                mNextSortingOrder = order;
                if (order > mSortingOrderLimit)
                {
                    throw new InvalidOperationException("Composed UI view exceeds its assigned sorting-order interval.");
                }
            }
        }

        /// <summary>
        /// Removes one dynamically composed child view and refreshes local sorting.
        /// </summary>
        /// <param name="subView">The child view to remove.</param>
        internal void RemoveSubView(UIView subView)
        {
            if (mSubViews == null || !mSubViews.Remove(subView))
            {
                return;
            }
            RefreshSortingOrder();
        }

        /// <summary>
        /// Clears every dynamically composed child view.
        /// </summary>
        internal void ClearSubViews()
        {
            mSubViews?.Clear();
        }

        /// <summary>
        /// Applies sorting metadata owned directly by this view.
        /// </summary>
        /// <param name="order">The next sorting order available to this view.</param>
        private void ApplyOwnSortingOrder(ref int order)
        {
            if (SortingNodes == null)
            {
                return;
            }
            var firstOrder = order;
            foreach (var node in SortingNodes)
            {
                var target = node.Target;
                if (!target)
                {
                    continue;
                }
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

        /// <summary>
        /// Reapplies this composed view tree inside its previously assigned sorting interval.
        /// </summary>
        private void RefreshSortingOrder()
        {
            if (!mHasSortingOrder)
            {
                return;
            }
            var order = mFirstSortingOrder;
            ApplySortingOrder(ref order, mSortingOrderLimit);
        }
    }
}
