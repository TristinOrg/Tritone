namespace Tritone.UI
{
    /// <summary>
    /// Defines the visual parent used by a window instance.
    /// </summary>
    public enum EUILayer
    {
        // Displays persistent background UI.
        Background,

        // Displays normal screens and panels.
        Normal,

        // Displays popup windows above normal UI.
        Popup,

        // Displays tutorials and input guidance.
        Guide,

        // Displays loading UI above gameplay windows.
        Loading,

        // Displays highest-priority system UI.
        System
    }
}
