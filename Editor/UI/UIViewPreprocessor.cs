using System;
using System.Collections.Generic;
using Tritone.Unity.UI;
using UnityEditor;
using UnityEngine;

namespace Tritone.Editor.UI
{
    /// <summary>
    /// Captures deterministic Canvas and Renderer hierarchy metadata for one UI prefab.
    /// </summary>
    internal static class UIViewPreprocessor
    {
        /// <summary>
        /// Stores one render component together with its authoring order and traversal index.
        /// </summary>
        private readonly struct SortingCandidate
        {
            /// <summary>
            /// Stores the Canvas or Renderer component.
            /// </summary>
            internal readonly Component Target;

            /// <summary>
            /// Stores the authored sorting order.
            /// </summary>
            internal readonly int Order;

            /// <summary>
            /// Stores the depth-first traversal index used to resolve equal orders.
            /// </summary>
            internal readonly int TraversalIndex;

            /// <summary>
            /// Stores the hierarchy depth relative to the prefab root.
            /// </summary>
            internal readonly int Depth;

            /// <summary>
            /// Creates one immutable preprocessing candidate.
            /// </summary>
            /// <param name="target">The render component.</param>
            /// <param name="order">The authored sorting order.</param>
            /// <param name="traversalIndex">The deterministic traversal index.</param>
            /// <param name="depth">The hierarchy depth.</param>
            internal SortingCandidate(Component target, int order, int traversalIndex, int depth)
            {
                Target         = target;
                Order          = order;
                TraversalIndex = traversalIndex;
                Depth          = depth;
            }
        }

        /// <summary>
        /// Preprocesses one UI prefab authoring component and updates an attached UIView when available.
        /// </summary>
        /// <param name="prefabRef">The prefab authoring component.</param>
        /// <returns>True when preprocessing completed; otherwise, false.</returns>
        internal static bool Process(UIPrefabRef prefabRef)
        {
            if (!prefabRef)
                return false;
            var candidates = new List<SortingCandidate>();
            var traversal  = 0;
            var valid      = true;
            Collect(prefabRef.transform, 0, ref traversal, candidates, ref valid);
            if (!valid)
                return false;
            candidates.Sort(CompareCandidates);

            var nodes = new UISortingNode[candidates.Count];
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                nodes[i] = new UISortingNode
                {
                    Target         = candidate.Target,
                    RelativeOrder  = i,
                    HierarchyDepth = candidate.Depth
                };
            }

            Undo.RecordObject(prefabRef, "Preprocess UIView sorting hierarchy");
            prefabRef.SortingNodes = nodes;
            EditorUtility.SetDirty(prefabRef);
            var view = prefabRef.GetComponent<UIView>();
            if (view)
            {
                Undo.RecordObject(view, "Preprocess UIView sorting hierarchy");
                view.SortingNodes = nodes;
                EditorUtility.SetDirty(view);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(prefabRef);
            AssetDatabase.SaveAssets();
            Debug.Log($"Preprocessed {prefabRef.name}: {nodes.Length} sorting nodes recorded.", prefabRef);
            return true;
        }

        /// <summary>
        /// Collects render nodes in deterministic hierarchy order.
        /// </summary>
        /// <param name="transform">The current hierarchy node.</param>
        /// <param name="depth">The current hierarchy depth.</param>
        /// <param name="traversal">The next traversal index.</param>
        /// <param name="candidates">The destination candidate list.</param>
        /// <param name="valid">Indicates whether every render node uses the default sorting layer.</param>
        private static void Collect(Transform transform, int depth, ref int traversal, List<SortingCandidate> candidates, ref bool valid)
        {
            var canvas = transform.GetComponent<Canvas>();
            if (canvas)
            {
                valid &= ValidateSortingLayer(canvas, canvas.sortingLayerID);
                candidates.Add(new SortingCandidate(canvas, canvas.sortingOrder, traversal++, depth));
            }
            var renderers = transform.GetComponents<Renderer>();
            foreach (var renderer in renderers)
            {
                valid &= ValidateSortingLayer(renderer, renderer.sortingLayerID);
                candidates.Add(new SortingCandidate(renderer, renderer.sortingOrder, traversal++, depth));
            }
            for (var i = 0; i < transform.childCount; i++)
                Collect(transform.GetChild(i), depth + 1, ref traversal, candidates, ref valid);
        }

        /// <summary>
        /// Rejects non-default sorting layers because sorting order intervals cannot cross sorting layers.
        /// </summary>
        /// <param name="component">The Canvas or Renderer being validated.</param>
        /// <param name="sortingLayerID">The authored sorting layer identifier.</param>
        /// <returns>True when the default sorting layer is used; otherwise, false.</returns>
        private static bool ValidateSortingLayer(Component component, int sortingLayerID)
        {
            if (sortingLayerID == 0)
                return true;
            Debug.LogError($"{component.name} uses a non-default sorting layer. Tritone UI sorting requires the default sorting layer.", component);
            return false;
        }

        /// <summary>
        /// Sorts by authored order and resolves ties with hierarchy traversal order.
        /// </summary>
        /// <param name="left">The left candidate.</param>
        /// <param name="right">The right candidate.</param>
        /// <returns>A standard comparison result.</returns>
        private static int CompareCandidates(SortingCandidate left, SortingCandidate right)
        {
            var result = left.Order.CompareTo(right.Order);
            return result != 0 ? result : left.TraversalIndex.CompareTo(right.TraversalIndex);
        }
    }
}
