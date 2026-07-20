using System;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Flows;

namespace Tritone.Kernel
{
    /// <summary>
    /// Exposes application flow transitions to one module context.
    /// </summary>
    public sealed class FlowCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Stores the shared flow service resolved on first use.
        private IFlowService mService;

        /// <summary>
        /// Initializes flow operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal FlowCapability(ModuleContext context)
        {
            mContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gets the concrete type of the active flow.
        /// </summary>
        public Type ActiveFlowType => Service.ActiveFlowType;

        /// <summary>
        /// Gets whether one flow transition is currently running.
        /// </summary>
        public bool IsSwitching => Service.IsSwitching;

        /// <summary>
        /// Switches to one explicitly registered flow.
        /// </summary>
        /// <typeparam name="TFlow">The concrete registered flow type.</typeparam>
        /// <param name="cancellationToken">Cancels entry before the target becomes active.</param>
        /// <returns>A task containing the active target flow.</returns>
        public Task<TFlow> SwitchAsync<TFlow>(
            CancellationToken cancellationToken = default)
            where TFlow : class, IFlow
        {
            return Service.SwitchAsync<TFlow>(cancellationToken);
        }

        /// <summary>
        /// Exits and releases the active flow.
        /// </summary>
        public void Exit()
        {
            Service.Exit();
        }

        /// <summary>
        /// Gets the configured shared flow service.
        /// </summary>
        private IFlowService Service =>
            mService ??= mContext.GetRequired<IFlowService>(
                "Flow infrastructure is unavailable.");
    }
}
