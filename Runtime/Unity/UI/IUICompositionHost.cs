using Tritone.Assets;
using Tritone.Pooling;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Receives optional application services used by window-local UI composition.
    /// </summary>
    internal interface IUICompositionHost
    {
        /// <summary>
        /// Configures services before the window is opened.
        /// </summary>
        /// <param name="assetService">The configured asset service, or null when unavailable.</param>
        /// <param name="poolService">The configured pool service, or null when unavailable.</param>
        void ConfigureComposition(IAssetService assetService, IPoolService poolService);

        /// <summary>
        /// Releases item and panel instances owned by the current window activity.
        /// </summary>
        void ReleaseCompositionActivity();

        /// <summary>
        /// Releases all composition instances and loaded prefab assets.
        /// </summary>
        void ReleaseComposition();
    }
}
