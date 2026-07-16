using System;
using System.Collections.Generic;
using Tritone.UI;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Tracks unique window types owned by one module.
    /// </summary>
    internal sealed class UIWindowScope : IUIWindowScope
    {
        // Stores unique window types owned by this scope.
        private readonly HashSet<Type> mWindowTypes = new();

        // Stores the shared UI module until this scope is released.
        private UIModule               mUIModule;

        /// <summary>
        /// Initializes one scope owned by the shared UI module.
        /// </summary>
        /// <param name="uiModule">The UI module receiving ownership changes.</param>
        internal UIWindowScope(UIModule uiModule)
        {
            mUIModule = uiModule ?? throw new ArgumentNullException(nameof(uiModule));
        }

        /// <inheritdoc />
        public void AddWindow(Type windowType,
                              string assetPath,
                              EUILayer layer,
                              EUIWindowLifetime lifetime)
        {
            ThrowIfDisposed();
            if (windowType == null)
                throw new ArgumentNullException(nameof(windowType));

            if (mWindowTypes.Contains(windowType))
            {
                mUIModule.ValidateWindowDefinition(windowType, assetPath, layer, lifetime);
                return;
            }

            mUIModule.RegisterAndAcquireWindow(windowType, assetPath, layer, lifetime);
            mWindowTypes.Add(windowType);
        }

        /// <summary>
        /// Releases every window owned by this module scope.
        /// </summary>
        public void Dispose()
        {
            if (mUIModule == null)
                return;

            foreach (var windowType in mWindowTypes)
                mUIModule.ReleaseWindow(windowType);
            mWindowTypes.Clear();
            mUIModule = null;
        }

        /// <summary>
        /// Rejects registration after this scope has been released.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (mUIModule == null)
                throw new ObjectDisposedException(nameof(UIWindowScope));
        }
    }
}
