using AfterHours.Entities;
using AfterHours.Flows;
using AfterHours.Models;
using AfterHours.Modules;
using AfterHours.UI;
using Tritone.Diagnostics;
using Tritone.Dispatching;
using Tritone.Kernel;
using Tritone.Unity;
using Tritone.Unity.Assets;
using Tritone.Unity.Audio;
using Tritone.Unity.Diagnostics;
using Tritone.Unity.Input;
using Tritone.Unity.Localization;
using Tritone.Unity.Pooling;
using Tritone.Unity.Saves;
using Tritone.Unity.Scenes;
using Tritone.Unity.Settings;
using Tritone.Unity.Tables;
using Tritone.Unity.UI;
using Tritone.Timing;
using Tritone.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AfterHours.Bootstrap
{
    /// <summary>Composes the offline profile and owns the persistent field interface.</summary>
    public class AfterHoursBootstrap : TritoneBootstrap
    {
        private AfterHoursScreenController mScreen;
        private RuntimeDiagnosticsOverlay mDiagnostics;

        public static void ReturnToSampleHub()
        {
            var activeBootstrap = FindObjectOfType<TritoneBootstrap>();
            if (activeBootstrap != null)
                Destroy(activeBootstrap.gameObject);
            SceneManager.LoadScene("SampleHub");
        }

        protected override void Awake()
        {
            mScreen = new AfterHoursScreenController();
            EnsureEventSystem();
            mDiagnostics = gameObject.AddComponent<RuntimeDiagnosticsOverlay>();
            mDiagnostics.ToggleKey = KeyCode.F3;
            mDiagnostics.Visible = false;
            base.Awake();
            var events = Application.Services.GetRequired<AfterHoursEvents>();
            var game = Application.Services.GetRequired<AfterHoursGameModule>();
            mScreen.Bind(game, events);
            _ = Application.Services.GetRequired<Tritone.Flows.IFlowService>().SwitchAsync<OfficeFlow>();
        }

        protected override void Configure(GameApplicationBuilder builder)
        {
            ConfigureAssetProvider(builder);
            ConfigureNetwork(builder);
            builder.UseRuntimeDiagnostics(ELogLevel.Debug, 256, 0.5d, new UnityLogSink())
                   .UseMainThreadDispatcher(256)
                   .UseTimers(64, 512)
                   .UseTweens(64, 512)
                   .UseEntities(64)
                   .AddSceneComponent<EvidenceMarkerComponent>()
                   .AddSceneComponent<CombatActorComponent>()
                   .AddSceneEntitySystem<SignalPresentationSystem>()
                   .UseTables()
                   .UseLocalization("en-US", language => $"Localization/{language}")
                   .UseSaves()
                   .UseSettings("after-hours-settings")
                   .UsePools()
                   .UseInput(new AfterHoursInputSource())
                   .UseAudio()
                   .UseScenes()
                   .UseUI(mScreen.Root);

            builder.AddApplicationModel<AfterHoursProfile>()
                   .AddSceneModel<CaseSessionModel>()
                   .AddFlow<OfficeFlow>()
                   .AddFlow<InvestigationFlow>()
                   .AddFlow<EncounterFlow>()
                   .AddFlow<DebriefFlow>()
                   .AddModule(new AfterHoursEventsModule())
                   .AddModule(new AfterHoursGameModule(),
                              typeof(AfterHoursEventsModule),
                              typeof(Tritone.Saves.SaveModule),
                              typeof(Tritone.Settings.SettingsModule),
                              typeof(Tritone.Localization.LocalizationModule),
                              typeof(Tritone.Unity.Input.InputModule),
                              typeof(AssetModule))
                   .AddSceneModule<OfficeSceneModule>()
                   .AddSceneModule<CaseSceneModule>()
                   .AddSceneModule<SystemsLabSceneModule>()
                   .UseInitialSceneModule<OfficeSceneModule>();
        }

        protected virtual void ConfigureAssetProvider(GameApplicationBuilder builder)
        {
            builder.UseAssets();
        }

        protected virtual void ConfigureNetwork(GameApplicationBuilder builder) { }

        protected override void Update()
        {
            base.Update();
            mScreen?.Tick();
        }

        protected override void OnDestroy()
        {
            mScreen?.Dispose();
            base.OnDestroy();
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
                return;
            var eventSystem = new GameObject("After Hours Event System",
                                             typeof(EventSystem),
                                             typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }
    }

    public sealed class AfterHoursEventsModule : ModuleBase
    {
        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton(new AfterHoursEvents());
        }
    }
}
