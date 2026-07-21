using System;
using Tritone.Dispatching;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides main-thread dispatch operations owned by one module context.
    /// </summary>
    public sealed class MainThreadCapability
    {
        /// <summary>
        /// Stores the owning module context.
        /// </summary>
        private readonly ModuleContext mContext;

        /// <summary>
        /// Lazily stores the domain-specific dispatch scope.
        /// </summary>
        private IMainThreadDispatchScope mScope;

        /// <summary>
        /// Initializes main-thread dispatch operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal MainThreadCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Posts one callback for the next dispatcher pre-update.
        /// </summary>
        /// <param name="callback">The callback to execute on the application thread.</param>
        /// <returns>A value handle that can cancel or query the pending callback.</returns>
        public DispatchHandle Post(Action callback)
        {
            return GetScope().Post(callback);
        }

        /// <summary>
        /// Cancels one pending callback owned by this module.
        /// </summary>
        /// <param name="handle">The pending callback handle.</param>
        /// <returns>True when the callback was pending and is now cancelled.</returns>
        public bool Cancel(DispatchHandle handle)
        {
            return mScope != null && mScope.Cancel(handle);
        }

        /// <summary>
        /// Determines whether one module-owned callback remains pending.
        /// </summary>
        /// <param name="handle">The callback handle to query.</param>
        /// <returns>True when the callback remains pending.</returns>
        public bool IsPending(DispatchHandle handle)
        {
            return mScope != null && mScope.IsPending(handle);
        }

        /// <summary>
        /// Cancels every callback owned by this module.
        /// </summary>
        public void CancelAll()
        {
            mScope?.CancelAll();
        }

        /// <summary>
        /// Gets or creates the dispatch scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned dispatch scope.</returns>
        private IMainThreadDispatchScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<IMainThreadDispatcherService>("Main-thread dispatch infrastructure is not configured. Call builder.UseMainThreadDispatcher() before posting callbacks.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }
    }
}
