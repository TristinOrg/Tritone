using System;
using Tritone.Timing;
using Tritone.Tweening;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides tween operations whose ownership follows one module context.
    /// </summary>
    public sealed class TweenCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the module-owned tween scope.
        private ITweenScope mScope;

        /// <summary>
        /// Initializes tween operations for one module context.
        /// </summary>
        internal TweenCapability(ModuleContext context)
        {
            mContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Plays one numeric interpolation.
        /// </summary>
        public TweenHandle Play(float from,
                                float to,
                                double duration,
                                Action<float> setter,
                                ETweenEase ease         = ETweenEase.Linear,
                                ETimerTimeMode timeMode = ETimerTimeMode.Scaled,
                                Action completed        = null)
        {
            return GetScope().Play(from,
                                   to,
                                   duration,
                                   setter,
                                   ease,
                                   timeMode,
                                   completed);
        }

        /// <summary>
        /// Plays one immutable sequence.
        /// </summary>
        public TweenHandle Play(TweenSequence sequence,
                                int loops                = 1,
                                ETimerTimeMode timeMode  = ETimerTimeMode.Scaled,
                                Action completed         = null)
        {
            return GetScope().Play(sequence, loops, timeMode, completed);
        }

        /// <summary>
        /// Schedules one callback after a delay.
        /// </summary>
        public TweenHandle Delay(double duration,
                                 Action completed,
                                 ETimerTimeMode timeMode = ETimerTimeMode.Scaled)
        {
            return GetScope().Delay(duration, completed, timeMode);
        }

        /// <summary>Pauses one active tween.</summary>
        public bool Pause(TweenHandle handle) => mScope != null && mScope.Pause(handle);

        /// <summary>Resumes one paused tween.</summary>
        public bool Resume(TweenHandle handle) => mScope != null && mScope.Resume(handle);

        /// <summary>Cancels one active tween.</summary>
        public bool Cancel(TweenHandle handle) => mScope != null && mScope.Cancel(handle);

        /// <summary>Determines whether one tween is active.</summary>
        public bool IsActive(TweenHandle handle) => mScope != null && mScope.IsActive(handle);

        /// <summary>Determines whether one tween is paused.</summary>
        public bool IsPaused(TweenHandle handle) => mScope != null && mScope.IsPaused(handle);

        /// <summary>Cancels every tween owned by this capability.</summary>
        public void CancelAll() => mScope?.CancelAll();

        /// <summary>
        /// Gets or creates the tween scope owned by this module context.
        /// </summary>
        private ITweenScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<ITweenService>(
                "Tween infrastructure is not configured. Call builder.UseTweens() before adding game modules.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }
    }
}
