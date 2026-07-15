namespace Tritone.Unity.UI
{
    /// <summary>
    /// Represents one UI region backed by a strongly typed prefab view.
    /// </summary>
    /// <typeparam name="TView">The view component attached to the panel prefab.</typeparam>
    public abstract class UIPanel<TView> : UIElement<TView> where TView : UIView
    {
    }
}
