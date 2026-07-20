using System;
using Tritone.Timing;

namespace Tritone.Tweening
{
    /// <summary>
    /// Owns scheduled tweens and cancels them when its lifetime ends.
    /// </summary>
    public interface ITweenScope : IDisposable
    {
        /// <summary>
        /// Plays one numeric interpolation.
        /// </summary>
        TweenHandle Play(float from,
                         float to,
                         double duration,
                         Action<float> setter,
                         ETweenEase ease                = ETweenEase.Linear,
                         ETimerTimeMode timeMode        = ETimerTimeMode.Scaled,
                         Action completed               = null);

        /// <summary>
        /// Plays one immutable sequence for a finite or infinite loop count.
        /// </summary>
        TweenHandle Play(TweenSequence sequence,
                         int loops                       = 1,
                         ETimerTimeMode timeMode         = ETimerTimeMode.Scaled,
                         Action completed                = null);

        /// <summary>
        /// Schedules one completion callback after a delay.
        /// </summary>
        TweenHandle Delay(double duration,
                          Action completed,
                          ETimerTimeMode timeMode = ETimerTimeMode.Scaled);

        /// <summary>Pauses one active tween.</summary>
        bool Pause(TweenHandle handle);

        /// <summary>Resumes one paused tween.</summary>
        bool Resume(TweenHandle handle);

        /// <summary>Cancels one active tween without invoking completion.</summary>
        bool Cancel(TweenHandle handle);

        /// <summary>Determines whether one tween is active.</summary>
        bool IsActive(TweenHandle handle);

        /// <summary>Determines whether one active tween is paused.</summary>
        bool IsPaused(TweenHandle handle);

        /// <summary>Cancels every tween owned by this scope.</summary>
        void CancelAll();
    }
}
