using System;
using System.Threading.Tasks;
using Tritone.UI;

namespace Tritone.Kernel
{

    /// <summary>
    /// Provides UI operations whose definition ownership follows one module context.
    /// </summary>
    public sealed class UICapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific window scope.
        private IUIWindowScope mScope;

        /// <summary>
        /// Initializes UI operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal UICapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Registers and owns one UI window definition.
        /// </summary>
        /// <param name="windowType">The concrete window component type.</param>
        /// <param name="assetPath">The provider path used to load its prefab.</param>
        /// <param name="layer">The visual layer receiving the window.</param>
        /// <param name="lifetime">The configured window lifetime.</param>
        public void AddWindow(Type windowType,
                              string assetPath,
                              EUILayer layer,
                              EUIWindowLifetime lifetime)
        {
            GetScope().AddWindow(windowType, assetPath, layer, lifetime);
        }

        /// <summary>
        /// Opens one registered window.
        /// </summary>
        /// <param name="windowType">The concrete registered window type.</param>
        /// <returns>The opened window instance.</returns>
        public object Open(Type windowType)
        {
            return GetService().OpenWindow(windowType);
        }

        /// <summary>
        /// Opens one registered window asynchronously.
        /// </summary>
        /// <param name="windowType">The concrete registered window type.</param>
        /// <returns>A task containing the opened window instance.</returns>
        public Task<object> OpenAsync(Type windowType)
        {
            return GetService().OpenWindowAsync(windowType);
        }

        /// <summary>
        /// Closes one registered window.
        /// </summary>
        /// <param name="windowType">The concrete registered window type.</param>
        /// <returns>True when the window was closed; otherwise, false.</returns>
        public bool Close(Type windowType)
        {
            return GetService().CloseWindow(windowType);
        }

        /// <summary>
        /// Gets one created window without opening it.
        /// </summary>
        /// <param name="windowType">The concrete registered window type.</param>
        /// <returns>The created instance, or null when unavailable.</returns>
        public object Get(Type windowType)
        {
            return GetService().GetWindow(windowType);
        }

        /// <summary>
        /// Determines whether one window is open.
        /// </summary>
        /// <param name="windowType">The concrete registered window type.</param>
        /// <returns>True when the window is open; otherwise, false.</returns>
        public bool IsOpen(Type windowType)
        {
            return GetService().IsWindowOpen(windowType);
        }

        /// <summary>
        /// Gets the configured UI service.
        /// </summary>
        /// <returns>The application UI service.</returns>
        private IUIService GetService()
        {
            return mContext.GetRequired<IUIService>(
                "UI infrastructure is not configured. Call builder.UseUI() before adding game modules.");
        }

        /// <summary>
        /// Gets or creates the UI definition scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned window scope.</returns>
        private IUIWindowScope GetScope()
        {
            if (mScope != null)
                return mScope;
            mScope = mContext.Scope.Own(GetService().CreateScope());
            return mScope;
        }
    }
}
