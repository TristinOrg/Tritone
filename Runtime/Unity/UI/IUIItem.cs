namespace Tritone.Unity.UI
{
    /// <summary>
    /// Exposes the view owned by a reusable UI item.
    /// </summary>
    public interface IUIItem
    {
        /// <summary>
        /// Gets the strongly bound item view through its common base type.
        /// </summary>
        UIView View { get; }
    }
}
