using System;

namespace Tritone.Dispatching
{
    /// <summary>
    /// Creates lifecycle-owned main-thread dispatch scopes.
    /// </summary>
    public interface IMainThreadDispatcherService
    {
        /// <summary>
        /// Creates one independently disposable dispatch scope.
        /// </summary>
        /// <returns>A new main-thread dispatch scope.</returns>
        IMainThreadDispatchScope CreateScope();
    }

    /// <summary>
    /// Owns callbacks posted to the main thread by one module lifetime.
    /// </summary>
    public interface IMainThreadDispatchScope : IDisposable
    {
        /// <summary>
        /// Posts one callback for the next dispatcher pre-update.
        /// </summary>
        /// <param name="callback">The callback to execute on the application thread.</param>
        /// <returns>A value handle that can cancel or query the pending callback.</returns>
        DispatchHandle Post(Action callback);

        /// <summary>
        /// Cancels one pending callback owned by this scope.
        /// </summary>
        /// <param name="handle">The pending callback handle.</param>
        /// <returns>True when the callback was pending and is now cancelled.</returns>
        bool Cancel(DispatchHandle handle);

        /// <summary>
        /// Determines whether one callback remains pending in this scope.
        /// </summary>
        /// <param name="handle">The callback handle to query.</param>
        /// <returns>True when the callback is still pending.</returns>
        bool IsPending(DispatchHandle handle);

        /// <summary>
        /// Cancels every callback currently owned by this scope.
        /// </summary>
        void CancelAll();
    }
}
