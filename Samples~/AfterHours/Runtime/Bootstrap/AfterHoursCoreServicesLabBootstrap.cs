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
            mTimers.SetTimer(new TimerKey("sample-delay"), 1d,
                () => mController?.SetResult("TIMER FIRED / Tritone delivered this callback on the next update."),
                ETimerTimeMode.Unscaled);
        }

        private void ResetProbe()
        {
            mTimers?.CancelAllTimers();
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
