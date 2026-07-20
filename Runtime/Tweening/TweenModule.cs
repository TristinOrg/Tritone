using System;
using System.Collections.Generic;
using Tritone.Kernel;
using Tritone.Timing;

namespace Tritone.Tweening
{
    /// <summary>
    /// Advances numeric tweens and immutable sequences without per-frame allocation.
    /// </summary>
    public sealed class TweenModule : ModuleBase, ITweenService, IUpdateSystem
    {
        /// <summary>
        /// Stores the shared no-op numeric setter used by delays.
        /// </summary>
        internal static readonly Action<float> IgnoreValue = OnIgnoreValue;

        // Maps active identifiers to scheduler slot indices.
        private readonly Dictionary<ulong, int> mIndices;

        // Stores reusable scheduler slot indices.
        private int[] mFreeIndices;

        // Stores scheduler entries.
        private TweenEntry[] mEntries;

        // Stores the number of reusable slots.
        private int mFreeCount;

        // Stores the first never-used scheduler slot.
        private int mHighWaterMark;

        // Stores the next unique non-zero identifier.
        private ulong mNextId;

        // Stores the current update generation for deferred starts.
        private uint mUpdateVersion;

        // Stores the maximum sequence steps processed in one update.
        private readonly int mMaxStepsPerUpdate;

        // Indicates whether this module has stopped permanently.
        private bool mStopped;

        /// <summary>
        /// Initializes the tween scheduler.
        /// </summary>
        /// <param name="capacity">The initial scheduler slot capacity.</param>
        /// <param name="maxStepsPerUpdate">The sequence step safety limit per update.</param>
        /// <param name="order">The normal update execution order.</param>
        public TweenModule(int capacity          = 64,
                           int maxStepsPerUpdate = 4096,
                           int order             = 0)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (maxStepsPerUpdate < 1)
                throw new ArgumentOutOfRangeException(nameof(maxStepsPerUpdate));

            mEntries           = new TweenEntry[capacity];
            mFreeIndices       = new int[capacity];
            mIndices           = new Dictionary<ulong, int>(capacity);
            mMaxStepsPerUpdate = maxStepsPerUpdate;
            Order              = order;
        }

        /// <inheritdoc />
        public int Order { get; }

        /// <inheritdoc />
        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton<ITweenService>(this);
        }

        /// <inheritdoc />
        public ITweenScope CreateScope()
        {
            if (mStopped)
                throw new InvalidOperationException(
                    "Tween scopes cannot be created after the tween module has stopped.");
            return new TweenScope(this);
        }

        /// <summary>
        /// Schedules one direct numeric tween.
        /// </summary>
        internal TweenHandle Play(TweenScope owner,
                                  float from,
                                  float to,
                                  double duration,
                                  Action<float> setter,
                                  ETweenEase ease,
                                  ETimerTimeMode timeMode,
                                  Action completed)
        {
            ValidateSchedule(owner, duration, setter, timeMode);
            TweenEntry entry = new()
            {
                Owner      = owner,
                Active     = true,
                TimeMode   = timeMode,
                Duration   = duration,
                From       = from,
                To         = to,
                Setter     = setter,
                Ease       = ease,
                Completed  = completed
            };
            return Schedule(in entry);
        }

        /// <summary>
        /// Schedules one immutable tween sequence.
        /// </summary>
        internal TweenHandle Play(TweenScope owner,
                                  TweenSequence sequence,
                                  int loops,
                                  ETimerTimeMode timeMode,
                                  Action completed)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));
            if (loops == 0 || loops < -1)
                throw new ArgumentOutOfRangeException(nameof(loops));
            ValidateTimeMode(timeMode);
            TweenEntry entry = new()
            {
                Owner          = owner,
                Active         = true,
                TimeMode       = timeMode,
                Sequence       = sequence,
                LoopsRemaining = loops,
                Completed      = completed
            };
            return Schedule(in entry);
        }

        /// <summary>
        /// Pauses or resumes one owned tween.
        /// </summary>
        internal bool SetPaused(TweenScope owner, TweenHandle handle, bool paused)
        {
            if (!TryGet(owner, handle, out var index))
                return false;
            mEntries[index].Paused = paused;
            return true;
        }

        /// <summary>
        /// Cancels one active owned tween without invoking completion.
        /// </summary>
        internal bool Cancel(TweenScope owner, TweenHandle handle)
        {
            if (!TryGet(owner, handle, out var index))
                return false;
            Release(index, false);
            return true;
        }

        /// <summary>
        /// Determines whether one owned tween is active.
        /// </summary>
        internal bool IsActive(TweenScope owner, TweenHandle handle)
        {
            return TryGet(owner, handle, out _);
        }

        /// <summary>
        /// Determines whether one owned active tween is paused.
        /// </summary>
        internal bool IsPaused(TweenScope owner, TweenHandle handle)
        {
            return TryGet(owner, handle, out var index) && mEntries[index].Paused;
        }

        /// <inheritdoc />
        public void Update(in FrameTime time)
        {
            mUpdateVersion++;
            if (mUpdateVersion == 0)
                mUpdateVersion++;

            var remainingSteps = mMaxStepsPerUpdate;
            var scanLength     = mHighWaterMark;
            for (var i = 0; i < scanLength; i++)
            {
                if (!mEntries[i].Active ||
                    mEntries[i].Paused ||
                    mEntries[i].CreatedUpdate == mUpdateVersion)
                    continue;

                var delta = mEntries[i].TimeMode == ETimerTimeMode.Scaled
                    ? Math.Max(0.0, time.DeltaTime)
                    : Math.Max(0.0, time.UnscaledDeltaTime);
                if (mEntries[i].Sequence == null)
                    ProcessDirect(i, delta);
                else
                    ProcessSequence(i, delta, ref remainingSteps);
                if (remainingSteps == 0)
                    break;
            }

            if (remainingSteps == 0)
                Logger.Warning(
                    $"Tween sequence step limit reached. Limit: {mMaxStepsPerUpdate}.");
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            mStopped = true;
            for (var i = 0; i < mHighWaterMark; i++)
                mEntries[i] = default;
            mIndices.Clear();
            mFreeCount     = 0;
            mHighWaterMark = 0;
            mNextId        = 0;
            mUpdateVersion = 0;
        }

        /// <summary>
        /// Processes one direct interpolation or delay.
        /// </summary>
        private void ProcessDirect(int index, double delta)
        {
            var id    = mEntries[index].Id;
            var entry = mEntries[index];
            entry.Elapsed = Math.Min(entry.Duration, entry.Elapsed + delta);
            var normalized = entry.Duration <= 0.0
                ? 1.0f
                : (float)(entry.Elapsed / entry.Duration);
            var eased = TweenEase.Evaluate(entry.Ease, normalized);
            var value = entry.From + (entry.To - entry.From) * eased;
            mEntries[index] = entry;
            try
            {
                entry.Setter.Invoke(value);
            }
            catch (Exception exception)
            {
                Logger.Error($"Tween setter failed. Id: {id}.", exception);
                if (IsSame(index, id))
                    Release(index, true);
                return;
            }

            if (entry.Elapsed >= entry.Duration && IsSame(index, id))
                Complete(index);
        }

        /// <summary>
        /// Processes sequence steps and carries unused frame time across boundaries.
        /// </summary>
        private void ProcessSequence(int index,
                                     double delta,
                                     ref int remainingSteps)
        {
            var id = mEntries[index].Id;
            while (remainingSteps > 0 && IsSame(index, id))
            {
                var entry = mEntries[index];
                var steps = entry.Sequence.Steps;
                if (entry.StepIndex >= steps.Length)
                {
                    remainingSteps--;
                    if (entry.LoopsRemaining == -1 || entry.LoopsRemaining > 1)
                    {
                        if (entry.LoopsRemaining > 1)
                            entry.LoopsRemaining--;
                        entry.StepIndex = 0;
                        entry.Elapsed   = 0.0;
                        mEntries[index] = entry;
                        continue;
                    }
                    Complete(index);
                    return;
                }

                var step = steps[entry.StepIndex];
                if (step.Kind == TweenStep.CallbackKind)
                {
                    remainingSteps--;
                    entry.StepIndex++;
                    mEntries[index] = entry;
                    if (!InvokeCallback(index, id, step.Callback, "sequence callback"))
                        return;
                    continue;
                }

                var available = Math.Max(0.0, step.Duration - entry.Elapsed);
                var consumed  = Math.Min(delta, available);
                entry.Elapsed += consumed;
                delta         -= consumed;
                var complete   = entry.Elapsed >= step.Duration;
                if (step.Kind == TweenStep.TweenKind)
                {
                    var normalized = step.Duration <= 0.0
                        ? 1.0f
                        : (float)(entry.Elapsed / step.Duration);
                    var eased = TweenEase.Evaluate(step.Ease, normalized);
                    var value = step.From + (step.To - step.From) * eased;
                    mEntries[index] = entry;
                    try
                    {
                        step.Setter.Invoke(value);
                    }
                    catch (Exception exception)
                    {
                        Logger.Error($"Tween sequence setter failed. Id: {id}.", exception);
                        if (IsSame(index, id))
                            Release(index, true);
                        return;
                    }
                    if (!IsSame(index, id))
                        return;
                }

                if (!complete)
                {
                    mEntries[index] = entry;
                    return;
                }

                remainingSteps--;
                entry.StepIndex++;
                entry.Elapsed   = 0.0;
                mEntries[index] = entry;
            }
        }

        /// <summary>
        /// Invokes one callback and cancels the tween when it fails.
        /// </summary>
        private bool InvokeCallback(int index,
                                    ulong id,
                                    Action callback,
                                    string category)
        {
            try
            {
                callback.Invoke();
                return IsSame(index, id);
            }
            catch (Exception exception)
            {
                Logger.Error($"Tween {category} failed. Id: {id}.", exception);
                if (IsSame(index, id))
                    Release(index, true);
                return false;
            }
        }

        /// <summary>
        /// Completes one tween, releases its slot, then invokes completion safely.
        /// </summary>
        private void Complete(int index)
        {
            var id        = mEntries[index].Id;
            var completed = mEntries[index].Completed;
            Release(index, true);
            if (completed == null)
                return;
            try
            {
                completed.Invoke();
            }
            catch (Exception exception)
            {
                Logger.Error($"Tween completion callback failed. Id: {id}.", exception);
            }
        }

        /// <summary>
        /// Adds one entry to a fresh or recycled slot.
        /// </summary>
        private TweenHandle Schedule(in TweenEntry source)
        {
            if (mStopped)
                throw new InvalidOperationException(
                    "Tweens cannot be scheduled after the tween module has stopped.");
            var index = mFreeCount > 0
                ? mFreeIndices[--mFreeCount]
                : AllocateSlot();
            var id = NextId();
            var entry = source;
            entry.Id            = id;
            entry.CreatedUpdate = mUpdateVersion;
            mEntries[index]     = entry;
            mIndices.Add(id, index);
            return new TweenHandle(id);
        }

        /// <summary>
        /// Releases one active slot and optionally updates its owner tracking.
        /// </summary>
        private void Release(int index, bool completed)
        {
            var entry  = mEntries[index];
            var handle = new TweenHandle(entry.Id);
            mIndices.Remove(entry.Id);
            mEntries[index] = default;
            mFreeIndices[mFreeCount++] = index;
            if (completed)
                entry.Owner.OnCompleted(handle);
        }

        /// <summary>
        /// Resolves one active handle owned by the supplied scope.
        /// </summary>
        private bool TryGet(TweenScope owner,
                            TweenHandle handle,
                            out int index)
        {
            if (!handle.IsValid ||
                !mIndices.TryGetValue(handle.Id, out index) ||
                !ReferenceEquals(mEntries[index].Owner, owner))
            {
                index = -1;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether a slot still contains one identifier after a callback.
        /// </summary>
        private bool IsSame(int index, ulong id)
        {
            return index >= 0 &&
                   index < mHighWaterMark &&
                   mEntries[index].Active &&
                   mEntries[index].Id == id;
        }

        /// <summary>
        /// Allocates one never-used slot and grows storage when required.
        /// </summary>
        private int AllocateSlot()
        {
            if (mHighWaterMark == mEntries.Length)
            {
                var next = mEntries.Length * 2;
                Array.Resize(ref mEntries, next);
                Array.Resize(ref mFreeIndices, next);
            }
            return mHighWaterMark++;
        }

        /// <summary>
        /// Allocates one unique active non-zero identifier.
        /// </summary>
        private ulong NextId()
        {
            do
            {
                mNextId++;
                if (mNextId == 0)
                    mNextId++;
            }
            while (mIndices.ContainsKey(mNextId));
            return mNextId;
        }

        /// <summary>
        /// Validates direct tween scheduling arguments.
        /// </summary>
        private static void ValidateSchedule(TweenScope owner,
                                             double duration,
                                             Action<float> setter,
                                             ETimerTimeMode timeMode)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (setter == null)
                throw new ArgumentNullException(nameof(setter));
            if (double.IsNaN(duration) || double.IsInfinity(duration) || duration < 0.0)
                throw new ArgumentOutOfRangeException(nameof(duration));
            ValidateTimeMode(timeMode);
        }

        /// <summary>
        /// Validates one supported tween clock.
        /// </summary>
        private static void ValidateTimeMode(ETimerTimeMode timeMode)
        {
            if (timeMode != ETimerTimeMode.Scaled &&
                timeMode != ETimerTimeMode.Unscaled)
                throw new ArgumentOutOfRangeException(nameof(timeMode));
        }

        /// <summary>
        /// Ignores one delay interpolation value.
        /// </summary>
        private static void OnIgnoreValue(float value) { }
    }
}
