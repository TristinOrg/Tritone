using System;
using Tritone.Timing;

namespace Tritone.Tweening
{
    /// <summary>
    /// Stores one scheduler slot without per-frame allocation.
    /// </summary>
    internal struct TweenEntry
    {
        internal ulong Id;
        internal TweenScope Owner;
        internal bool Active;
        internal bool Paused;
        internal uint CreatedUpdate;
        internal ETimerTimeMode TimeMode;
        internal double Elapsed;
        internal double Duration;
        internal float From;
        internal float To;
        internal Action<float> Setter;
        internal ETweenEase Ease;
        internal Action Completed;
        internal TweenSequence Sequence;
        internal int StepIndex;
        internal int LoopsRemaining;
    }
}
