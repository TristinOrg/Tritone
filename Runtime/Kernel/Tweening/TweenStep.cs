using System;

namespace Tritone.Tweening
{
    /// <summary>
    /// Stores one immutable tween sequence operation.
    /// </summary>
    public readonly struct TweenStep
    {
        /// <summary>Identifies a numeric interpolation step.</summary>
        internal const byte TweenKind = 1;
        /// <summary>Identifies a time-only delay step.</summary>
        internal const byte DelayKind = 2;
        /// <summary>Identifies an immediate callback step.</summary>
        internal const byte CallbackKind = 3;

        /// <summary>Stores the internal step category.</summary>
        internal readonly byte Kind;
        /// <summary>Stores the interpolation start value.</summary>
        internal readonly float From;
        /// <summary>Stores the interpolation target value.</summary>
        internal readonly float To;
        /// <summary>Stores the step duration in seconds.</summary>
        internal readonly double Duration;
        /// <summary>Stores the interpolation target callback.</summary>
        internal readonly Action<float> Setter;
        /// <summary>Stores the easing curve.</summary>
        internal readonly ETweenEase Ease;
        /// <summary>Stores the immediate callback.</summary>
        internal readonly Action Callback;

        /// <summary>
        /// Initializes one internal sequence step.
        /// </summary>
        private TweenStep(byte kind,
                          float from,
                          float to,
                          double duration,
                          Action<float> setter,
                          ETweenEase ease,
                          Action callback)
        {
            Kind     = kind;
            From     = from;
            To       = to;
            Duration = duration;
            Setter   = setter;
            Ease     = ease;
            Callback = callback;
        }

        /// <summary>
        /// Creates one numeric interpolation step.
        /// </summary>
        public static TweenStep Tween(float from,
                                      float to,
                                      double duration,
                                      Action<float> setter,
                                      ETweenEase ease = ETweenEase.Linear)
        {
            ValidateDuration(duration);
            if (setter == null)
                throw new ArgumentNullException(nameof(setter));
            return new TweenStep(TweenKind, from, to, duration, setter, ease, null);
        }

        /// <summary>
        /// Creates one time-only delay step.
        /// </summary>
        public static TweenStep Delay(double duration)
        {
            ValidateDuration(duration);
            return new TweenStep(DelayKind, 0.0f, 0.0f, duration, null, default, null);
        }

        /// <summary>
        /// Creates one immediate callback step.
        /// </summary>
        public static TweenStep Call(Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            return new TweenStep(CallbackKind, 0.0f, 0.0f, 0.0, null, default, callback);
        }

        /// <summary>
        /// Validates one finite non-negative duration.
        /// </summary>
        private static void ValidateDuration(double duration)
        {
            if (double.IsNaN(duration) || double.IsInfinity(duration) || duration < 0.0)
                throw new ArgumentOutOfRangeException(nameof(duration));
        }
    }
}
