using System;
using UnityEngine;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Describes one strongly typed serialized field generated for a UI view.
    /// </summary>
    [Serializable]
    public struct UIViewReference
    {
        /// <summary>
        /// Stores the generated field name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Stores the referenced GameObject or Component.
        /// </summary>
        public UnityEngine.Object Value;

        /// <summary>
        /// Stores the XML documentation emitted above the generated field.
        /// </summary>
        public string Description;
    }
}
