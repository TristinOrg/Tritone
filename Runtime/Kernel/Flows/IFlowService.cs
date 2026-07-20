using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Flows
{
    /// <summary>
    /// Coordinates the single application flow active at one time.
    /// </summary>
    public interface IFlowService
    {
        /// <summary>
        /// Gets the concrete type of the active flow.
        /// </summary>
        Type ActiveFlowType { get; }

        /// <summary>
        /// Gets whether one flow transition is currently running.
        /// </summary>
        bool IsSwitching { get; }

        /// <summary>
        /// Switches to one explicitly registered flow.
        /// </summary>
        /// <typeparam name="TFlow">The concrete registered flow type.</typeparam>
        /// <param name="cancellationToken">Cancels entry before the target becomes active.</param>
        /// <returns>A task containing the active target flow.</returns>
        Task<TFlow> SwitchAsync<TFlow>(
            CancellationToken cancellationToken = default)
            where TFlow : class, IFlow;

        /// <summary>
        /// Switches to one explicitly registered flow by runtime type.
        /// </summary>
        /// <param name="flowType">The concrete registered flow type.</param>
        /// <param name="cancellationToken">Cancels entry before the target becomes active.</param>
        /// <returns>A task containing the active target flow.</returns>
        Task<IFlow> SwitchAsync(
            Type flowType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exits and releases the active flow.
        /// </summary>
        void Exit();
    }
}
