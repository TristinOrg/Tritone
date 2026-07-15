namespace Tritone.Unity.UI
{
    /// <summary>
    /// Represents a complete UI window backed by a strongly typed prefab view.
    /// </summary>
    /// <typeparam name="TView">The view component attached to the window prefab.</typeparam>
    public abstract class UIWindow<TView> : UIElement<TView> where TView : UIView
    {
        /// <summary>
        /// Activates this window and enters its binding and open stages.
        /// </summary>
        public virtual void Open()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Deactivates this window and enters its close and automatic unbinding stages.
        /// </summary>
        public virtual void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
