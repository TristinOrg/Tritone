using System;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Kernel;

namespace Tritone.Flows
{
    /// <summary>
    /// Defines one application flow with asynchronous entry and deterministic exit.
    /// </summary>
    public interface IFlow : IDisposable
    {
        /// <summary>
        /// Enters the flow and completes when it is ready to become active.
        /// </summary>
        /// <param name="cancellationToken">Cancels the pending flow entry.</param>
        /// <returns>A task that completes when entry finishes.</returns>
        Task EnterAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Updates the active flow once per application frame.
        /// </summary>
        /// <param name="time">The immutable timing data for the current frame.</param>
        void Update(in FrameTime time);

        /// <summary>
        /// Exits the active flow before another flow enters.
        /// </summary>
        void Exit();
    }
}
