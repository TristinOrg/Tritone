using System;
using System.Threading.Tasks;

namespace Tritone.Content
{
    /// <summary>
    /// Creates isolated module-owned content update lifetimes.
    /// </summary>
    public interface IContentUpdateService
    {
        /// <summary>
        /// Creates one independently cancellable content update scope.
        /// </summary>
        /// <returns>A new content update scope.</returns>
        IContentUpdateScope CreateScope();
    }

    /// <summary>
    /// Owns content update cancellation for one module lifetime.
    /// </summary>
    public interface IContentUpdateScope : IDisposable
    {
        /// <summary>
        /// Checks, downloads, verifies, and activates the latest remote content.
        /// </summary>
        /// <param name="progress">The optional allocation-free progress callback.</param>
        /// <returns>A task containing the active manifest and executed plan.</returns>
        Task<ContentUpdateResult> UpdateAsync(Action<ContentUpdateProgress> progress = null);

        /// <summary>
        /// Cancels the active update owned by this scope.
        /// </summary>
        void Cancel();
    }
}
