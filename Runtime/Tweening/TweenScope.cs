using System;
using System.Collections.Generic;
using Tritone.Timing;

namespace Tritone.Tweening
{
    /// <summary>
    /// Tracks every tween scheduled by one lifetime owner.
    /// </summary>
    internal sealed class TweenScope : ITweenScope
    {
        // Stores active handles for complete owner cleanup.
        private readonly List<TweenHandle> mHandles = new(8);

        // Stores the shared scheduler module.
        private TweenModule mModule;

        /// <summary>
        /// Initializes one scope backed by a tween module.
        /// </summary>
        internal TweenScope(TweenModule module)
        {
            mModule = module ?? throw new ArgumentNullException(nameof(module));
        }

        /// <inheritdoc />
        public TweenHandle Play(float from,
                                float to,
                                double duration,
                                Action<float> setter,
                                ETweenEase ease         = ETweenEase.Linear,
                                ETimerTimeMode timeMode = ETimerTimeMode.Scaled,
                                Action completed        = null)
        {
            ThrowIfDisposed();
            var handle = mModule.Play(this,
                                      from,
                                      to,
                                      duration,
                                      setter,
                                      ease,
                                      timeMode,
                                      completed);
            mHandles.Add(handle);
            return handle;
        }

        /// <inheritdoc />
        public TweenHandle Play(TweenSequence sequence,
                                int loops                = 1,
                                ETimerTimeMode timeMode  = ETimerTimeMode.Scaled,
                                Action completed         = null)
        {
            ThrowIfDisposed();
            var handle = mModule.Play(this, sequence, loops, timeMode, completed);
            mHandles.Add(handle);
            return handle;
        }

        /// <inheritdoc />
        public TweenHandle Delay(double duration,
                                 Action completed,
                                 ETimerTimeMode timeMode = ETimerTimeMode.Scaled)
        {
            return Play(0.0f,
                        0.0f,
                        duration,
                        TweenModule.IgnoreValue,
                        ETweenEase.Linear,
                        timeMode,
                        completed);
        }

        /// <inheritdoc />
        public bool Pause(TweenHandle handle) =>
            mModule != null && mModule.SetPaused(this, handle, true);

        /// <inheritdoc />
        public bool Resume(TweenHandle handle) =>
            mModule != null && mModule.SetPaused(this, handle, false);

        /// <inheritdoc />
        public bool Cancel(TweenHandle handle)
        {
            if (mModule == null || !mModule.Cancel(this, handle))
                return false;
            Remove(handle);
            return true;
        }

        /// <inheritdoc />
        public bool IsActive(TweenHandle handle) =>
            mModule != null && mModule.IsActive(this, handle);

        /// <inheritdoc />
        public bool IsPaused(TweenHandle handle) =>
            mModule != null && mModule.IsPaused(this, handle);

        /// <inheritdoc />
        public void CancelAll()
        {
            if (mModule == null)
                return;
            for (int i = mHandles.Count - 1; i >= 0; i--)
                mModule.Cancel(this, mHandles[i]);
            mHandles.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (mModule == null)
                return;
            CancelAll();
            mModule = null;
        }

        /// <summary>
        /// Removes a naturally completed handle from owner tracking.
        /// </summary>
        internal void OnCompleted(TweenHandle handle)
        {
            Remove(handle);
        }

        /// <summary>
        /// Removes one handle by compact swap removal.
        /// </summary>
        private void Remove(TweenHandle handle)
        {
            for (int i = mHandles.Count - 1; i >= 0; i--)
            {
                if (mHandles[i] != handle)
                    continue;
                var lastIndex = mHandles.Count - 1;
                mHandles[i] = mHandles[lastIndex];
                mHandles.RemoveAt(lastIndex);
                return;
            }
        }

        /// <summary>
        /// Rejects scheduling after this scope has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (mModule == null)
                throw new ObjectDisposedException(nameof(TweenScope));
        }
    }
}
