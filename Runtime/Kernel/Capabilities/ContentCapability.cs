using System;
using System.Threading.Tasks;
using Tritone.Content;

namespace Tritone.Kernel
{

    /// <summary>
    /// Provides content update operations whose cancellation follows one module context.
    /// </summary>
    public sealed class ContentCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific content update scope.
        private IContentUpdateScope mScope;

        /// <summary>
        /// Initializes content update operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal ContentCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Runs one module-owned remote content update.
        /// </summary>
        /// <param name="progress">The optional update progress callback.</param>
        /// <returns>A task containing the activated content result.</returns>
        public Task<ContentUpdateResult> UpdateAsync(
            Action<ContentUpdateProgress> progress = null)
        {
            return GetScope().UpdateAsync(progress);
        }

        /// <summary>
        /// Starts one callback-based module-owned content update.
        /// </summary>
        /// <param name="completed">The callback invoked after successful activation.</param>
        /// <param name="progress">The optional update progress callback.</param>
        /// <param name="failed">The optional failure callback.</param>
        public void Start(Action<ContentUpdateResult> completed,
                          Action<ContentUpdateProgress> progress = null,
                          Action<Exception> failed              = null)
        {
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));
            _ = RunAsync(completed, progress, failed);
        }

        /// <summary>
        /// Cancels the active module-owned content update.
        /// </summary>
        public void Cancel()
        {
            mScope?.Cancel();
        }

        /// <summary>
        /// Gets or creates the content scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned content update scope.</returns>
        private IContentUpdateScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<IContentUpdateService>(
                "Content update infrastructure is not configured. Call builder.UseContentAssets() before adding game modules.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }

        /// <summary>
        /// Observes one callback-based update and contains callback failures.
        /// </summary>
        /// <param name="completed">The success callback.</param>
        /// <param name="progress">The optional progress callback.</param>
        /// <param name="failed">The optional failure callback.</param>
        /// <returns>A task that observes the complete update operation.</returns>
        private async Task RunAsync(Action<ContentUpdateResult> completed,
                                    Action<ContentUpdateProgress> progress,
                                    Action<Exception> failed)
        {
            try
            {
                var result = await UpdateAsync(progress);
                completed.Invoke(result);
            }
            catch (OperationCanceledException)
            {
                // Module-owned cancellation is an expected lifecycle outcome.
            }
            catch (Exception exception)
            {
                if (failed == null)
                {
                    mContext.Logger.Error("Content update failed.", exception);
                    return;
                }

                try
                {
                    failed.Invoke(exception);
                }
                catch (Exception callbackException)
                {
                    mContext.Logger.Error(
                        "Content update failure callback threw an exception.",
                        callbackException);
                }
            }
        }
    }
}
