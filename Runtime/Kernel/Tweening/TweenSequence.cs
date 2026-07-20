using System;
using System.Collections.Generic;

namespace Tritone.Tweening
{
    /// <summary>
    /// Stores an immutable sequence of tween, delay, and callback steps.
    /// </summary>
    public sealed class TweenSequence
    {
        /// <summary>
        /// Initializes one immutable sequence from copied steps.
        /// </summary>
        /// <param name="steps">The non-empty sequence steps.</param>
        public TweenSequence(params TweenStep[] steps)
        {
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));
            if (steps.Length == 0)
                throw new ArgumentException("A tween sequence requires at least one step.", nameof(steps));
            Steps = (TweenStep[])steps.Clone();
        }

        /// <summary>Gets the number of sequence steps.</summary>
        public int Count => Steps.Length;

        /// <summary>Stores immutable scheduler steps.</summary>
        internal TweenStep[] Steps { get; }
    }

    /// <summary>
    /// Builds one tween sequence outside per-frame hot paths.
    /// </summary>
    public sealed class TweenSequenceBuilder
    {
        // Stores steps until the immutable sequence is built.
        private readonly List<TweenStep> mSteps = new();

        /// <summary>Appends one numeric interpolation step.</summary>
        public TweenSequenceBuilder Append(float from,
                                           float to,
                                           double duration,
                                           Action<float> setter,
                                           ETweenEase ease = ETweenEase.Linear)
        {
            mSteps.Add(TweenStep.Tween(from, to, duration, setter, ease));
            return this;
        }

        /// <summary>Appends one time-only delay.</summary>
        public TweenSequenceBuilder AppendDelay(double duration)
        {
            mSteps.Add(TweenStep.Delay(duration));
            return this;
        }

        /// <summary>Appends one immediate callback.</summary>
        public TweenSequenceBuilder AppendCallback(Action callback)
        {
            mSteps.Add(TweenStep.Call(callback));
            return this;
        }

        /// <summary>Builds an immutable reusable sequence.</summary>
        public TweenSequence Build()
        {
            return new TweenSequence(mSteps.ToArray());
        }
    }
}
