using UnityEngine;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Represents one UI region backed by a strongly typed prefab view.
    /// </summary>
    /// <typeparam name="TView">The view component attached to the panel prefab.</typeparam>
    public abstract class UIPanel<TView> : UIElement<TView>, IUIPanel where TView : UIView
    {
        /// <inheritdoc />
        public GameObject GameObject => gameObject;

        /// <inheritdoc />
        public UIView View => ResolveView();

        /// <inheritdoc />
        public virtual void Open()
        {
            gameObject.SetActive(true);
        }

        /// <inheritdoc />
        public virtual void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
