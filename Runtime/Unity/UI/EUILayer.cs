namespace Tritone.Unity.UI
{
    /// <summary>
    /// Defines the visual parent used by a window instance.
    /// </summary>
    public enum EUILayer
    {
        /// <summary>Displays persistent background UI.</summary>
        Background,

        /// <summary>Displays normal screens and panels.</summary>
        Normal,

        /// <summary>Displays popup windows above normal UI.</summary>
        Popup,

        /// <summary>Displays tutorials and input guidance.</summary>
        Guide,

        /// <summary>Displays loading UI above gameplay windows.</summary>
        Loading,

        /// <summary>Displays highest-priority system UI.</summary>
        System
    }
}
