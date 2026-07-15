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
        private readonly HashSet<Type> mWindowTypes = new();
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
        public void AddWindow(Type windowType)
        {
            if (mUIModule == null)
                throw new ObjectDisposedException(nameof(UIWindowScope));
            if (windowType == null)
                throw new ArgumentNullException(nameof(windowType));
            if (!mWindowTypes.Add(windowType))
                return;

            try
            {
                mUIModule.AcquireWindow(windowType);
            }
            catch
            {
                mWindowTypes.Remove(windowType);
                throw;
            }
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
    }
}
