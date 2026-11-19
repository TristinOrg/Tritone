using AfterHours.UI;
using Tritone.Dispatching;
using Tritone.Diagnostics;
using Tritone.Kernel;
using Tritone.Timing;
using Tritone.Unity;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AfterHours.Bootstrap
{
    /// <summary>Demonstrates a Tritone-owned one-shot timer without starting the RPG.</summary>
    public sealed class AfterHoursCoreServicesLabBootstrap : TritoneBootstrap
    {
        private AfterHoursFeatureLabController mController;
        private ITimerScope mTimers;
        private float mStartedAt = -1f;

        protected override void Configure(GameApplicationBuilder builder)
        {
            builder.UseTimers().UseMainThreadDispatcher()
                   .UseRuntimeDiagnostics(ELogLevel.Debug, 64, 0.5d);
        }

        protected override void Awake()
        {
            EnsureEventSystem();
            base.Awake();
            mTimers = Application.Services.GetRequired<ITimerService>().CreateScope();
            mController = new AfterHoursFeatureLabController("TimerLab", RunProbe, ResetProbe);
            mController.Show();
        }

        private void RunProbe()
        {
            mController.SetResult("TIMER SCHEDULED / waiting 1 second…");
            mStartedAt = Time.unscaledTime;
            mController.SetProgress(0f);
            mTimers.SetTimer(new TimerKey("sample-delay"), 1d,
                () =>
                {
                    mStartedAt = -1f;
                    mController?.SetProgress(1f);
                    mController?.SetResult("TIMER FIRED / Tritone delivered the scheduled callback.");
                },
                ETimerTimeMode.Unscaled);
        }

        protected override void Update()
        {
            base.Update();
            if (mStartedAt >= 0f)
                mController?.SetProgress(Time.unscaledTime - mStartedAt);
        }

        private void ResetProbe()
        {
            mTimers?.CancelAllTimers();
            mStartedAt = -1f;
            mController?.SetProgress(0f);
            mController?.SetResult("READY / click Run Timer to schedule a one-shot callback.");
        }

        protected override void OnDestroy()
        {
            mTimers?.Dispose();
            mTimers = null;
            mController?.Dispose();
            mController = null;
            base.OnDestroy();
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("After Hours Event System", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
