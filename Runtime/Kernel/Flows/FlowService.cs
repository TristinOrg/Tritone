using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Kernel;

namespace Tritone.Flows
{
    /// <summary>
    /// Creates fresh flows and performs atomic transitions with rollback.
    /// </summary>
    internal sealed class FlowService : IFlowService, IDisposable
    {
        // Stores immutable flow registrations by concrete type.
        private readonly Dictionary<Type, FlowRegistration> mRegistrations;

        // Cancels pending flow entry when the application stops.
        private readonly CancellationTokenSource mLifetime = new();

        // Stores the currently active flow instance.
        private IFlow mActiveFlow;

        // Stores the concrete type of the active flow.
        private Type mActiveFlowType;

        // Stores the shared task for the transition in progress.
        private Task<IFlow> mTransition;

        // Stores the target type of the transition in progress.
        private Type mTransitionTarget;

        // Indicates whether this service has completed disposal.
        private bool mDisposed;

        /// <summary>
        /// Initializes the service with explicit immutable registrations.
        /// </summary>
        /// <param name="registrations">The validated flow registrations.</param>
        internal FlowService(FlowRegistration[] registrations)
        {
            if (registrations == null)
                throw new ArgumentNullException(nameof(registrations));

            mRegistrations = new Dictionary<Type, FlowRegistration>(
                registrations.Length);
            for (int i = 0, cnt = registrations.Length; i < cnt; i++)
                mRegistrations.Add(registrations[i].FlowType, registrations[i]);
        }

        /// <inheritdoc />
        public Type ActiveFlowType => mActiveFlowType;

        /// <inheritdoc />
        public bool IsSwitching => mTransition != null;

        /// <inheritdoc />
        public async Task<TFlow> SwitchAsync<TFlow>(
            CancellationToken cancellationToken = default)
            where TFlow : class, IFlow
        {
            return (TFlow)await SwitchAsync(
                typeof(TFlow),
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IFlow> SwitchAsync(
            Type flowType,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (flowType == null)
                throw new ArgumentNullException(nameof(flowType));
            if (!mRegistrations.TryGetValue(flowType, out var registration))
                throw new InvalidOperationException(
                    $"Flow '{flowType.FullName}' is not registered.");
            if (mTransition != null)
            {
                if (mTransitionTarget == flowType)
                    return mTransition;
                throw new InvalidOperationException(
                    $"Cannot enter flow '{flowType.FullName}' while " +
                    $"'{mTransitionTarget.FullName}' is entering.");
            }
            if (mActiveFlowType == flowType)
                return Task.FromResult(mActiveFlow);

            TaskCompletionSource<IFlow> completion = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            mTransitionTarget = flowType;
            mTransition       = completion.Task;
            _ = RunTransitionAsync(
                registration,
                cancellationToken,
                completion);
            return completion.Task;
        }

        /// <inheritdoc />
        public void Exit()
        {
            ThrowIfDisposed();
            if (mTransition != null)
                throw new InvalidOperationException(
                    "The active flow cannot exit during a transition.");

            ReleaseActiveFlow();
        }

        /// <summary>
        /// Updates the active flow when no transition is running.
        /// </summary>
        /// <param name="time">The immutable timing data for the current frame.</param>
        internal void Update(in FrameTime time)
        {
            if (mTransition == null)
                mActiveFlow?.Update(in time);
        }

        /// <summary>
        /// Cancels pending entry and releases the active flow.
        /// </summary>
        public void Dispose()
        {
            if (mDisposed)
                return;

            mLifetime.Cancel();
            try
            {
                mTransition?.GetAwaiter().GetResult();
            }
            catch
            {
                // Transition cleanup and rollback are completed before its task faults.
            }

            mDisposed = true;
            try
            {
                ReleaseActiveFlow();
            }
            finally
            {
                mLifetime.Dispose();
            }
        }

        /// <summary>
        /// Performs one transition and completes every shared caller task.
        /// </summary>
        /// <param name="registration">The target flow registration.</param>
        /// <param name="cancellationToken">The initiating caller cancellation token.</param>
        /// <param name="completion">The shared transition completion.</param>
        private async Task RunTransitionAsync(
            FlowRegistration registration,
            CancellationToken cancellationToken,
            TaskCompletionSource<IFlow> completion)
        {
            try
            {
                using (CancellationTokenSource linked =
                       CancellationTokenSource.CreateLinkedTokenSource(
                           mLifetime.Token,
                           cancellationToken))
                {
                    var result = await TransitionCoreAsync(
                        registration,
                        linked.Token).ConfigureAwait(false);
                    mTransition       = null;
                    mTransitionTarget = null;
                    completion.TrySetResult(result);
                }
            }
            catch (OperationCanceledException)
            {
                mTransition       = null;
                mTransitionTarget = null;
                completion.TrySetCanceled();
            }
            catch (Exception exception)
            {
                mTransition       = null;
                mTransitionTarget = null;
                completion.TrySetException(exception);
            }
        }

        /// <summary>
        /// Exits the previous flow, enters the target, and restores the previous flow on failure.
        /// </summary>
        /// <param name="registration">The target flow registration.</param>
        /// <param name="cancellationToken">Cancels target entry.</param>
        /// <returns>The successfully entered target flow.</returns>
        private async Task<IFlow> TransitionCoreAsync(
            FlowRegistration registration,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var previous     = mActiveFlow;
            var previousType = mActiveFlowType;
            var previousExited = false;
            IFlow target       = null;
            try
            {
                if (previous != null)
                {
                    previous.Exit();
                    previousExited = true;
                }

                cancellationToken.ThrowIfCancellationRequested();
                target = registration.Factory();
                if (target == null || target.GetType() != registration.FlowType)
                    throw new InvalidOperationException(
                        $"Flow factory for '{registration.FlowType.FullName}' " +
                        "returned an invalid instance.");

                await target.EnterAsync(cancellationToken).ConfigureAwait(false);
                mActiveFlow     = target;
                mActiveFlowType = registration.FlowType;
                previous?.Dispose();
                return target;
            }
            catch (Exception transitionException)
            {
                Exception cleanupException = null;
                if (target != null)
                {
                    try
                    {
                        target.Dispose();
                    }
                    catch (Exception exception)
                    {
                        cleanupException = exception;
                    }
                }

                if (previousExited)
                {
                    try
                    {
                        await previous.EnterAsync(
                            CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception rollbackException)
                    {
                        mActiveFlow     = null;
                        mActiveFlowType = null;
                        try
                        {
                            previous.Dispose();
                        }
                        catch (Exception exception)
                        {
                            rollbackException = new AggregateException(
                                rollbackException,
                                exception);
                        }

                        cleanupException = cleanupException == null
                            ? rollbackException
                            : new AggregateException(
                                cleanupException,
                                rollbackException);
                    }
                }

                if (cleanupException != null)
                    throw new AggregateException(
                        "Flow transition and rollback failed.",
                        transitionException,
                        cleanupException);
                throw;
            }
        }

        /// <summary>
        /// Exits and disposes the current flow, clearing active state first.
        /// </summary>
        private void ReleaseActiveFlow()
        {
            var flow        = mActiveFlow;
            mActiveFlow     = null;
            mActiveFlowType = null;
            if (flow == null)
                return;

            try
            {
                flow.Exit();
            }
            finally
            {
                flow.Dispose();
            }
        }

        /// <summary>
        /// Rejects access after application shutdown.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (mDisposed)
                throw new ObjectDisposedException(nameof(FlowService));
        }
    }
}
