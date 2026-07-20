using System;
using Tritone.Input;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides input bindings whose ownership follows one module context.
    /// </summary>
    public sealed class InputCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific input scope.
        private IInputScope mScope;

        /// <summary>
        /// Initializes input operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal InputCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Binds one named button-down callback.
        /// </summary>
        /// <param name="action">The configured input action name.</param>
        /// <param name="callback">The callback invoked on button down.</param>
        public void Bind(string action, Action callback)
        {
            GetScope().BindButton(action, callback);
        }

        /// <summary>
        /// Binds one named axis callback.
        /// </summary>
        /// <param name="action">The configured input action name.</param>
        /// <param name="callback">The callback invoked after a meaningful value change.</param>
        /// <param name="deadZone">The minimum meaningful axis magnitude and change.</param>
        public void BindAxis(string action, Action<float> callback, float deadZone)
        {
            GetScope().BindAxis(action, callback, deadZone);
        }

        /// <summary>
        /// Gets or creates the input scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned input scope.</returns>
        private IInputScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<IInputService>(
                "Input is not configured. Call builder.UseInput() before binding actions.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }
    }

}
