using UnityEngine;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Stores UI prefab authoring references and preprocessed render hierarchy metadata.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIPrefabRef : MonoBehaviour
    {
        /// <summary>
        /// Stores an optional generated class name; an empty value derives it from the GameObject name.
        /// </summary>
        public string ClassName;

        /// <summary>
        /// Stores the namespace used by the generated view source.
        /// </summary>
        public string Namespace = "Game.UI";

        /// <summary>
        /// Stores the project-relative directory receiving generated view sources.
        /// </summary>
        public string OutputDirectory = "Assets/Generated/Tritone/UI";

        /// <summary>
        /// Stores the fields exposed by the generated view.
        /// </summary>
        public UIViewReference[] References;

        /// <summary>
        /// Stores Canvas and Renderer nodes captured by the UI preprocessor.
        /// </summary>
        public UISortingNode[] SortingNodes;
    }
}
