namespace Tritone.Unity.UI
{
    /// <summary>
    /// Represents a reusable UI item backed by a strongly typed prefab view.
    /// </summary>
    /// <typeparam name="TView">The view component attached to the item prefab.</typeparam>
    public abstract class UIItem<TView> : UIElement<TView> where TView : UIView
    {
    }
}
