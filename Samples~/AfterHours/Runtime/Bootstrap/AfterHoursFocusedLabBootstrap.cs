using System;
using System.Threading;
using System.Threading.Tasks;
using AfterHours.UI;
using Tritone.Audio;
using Tritone.Diagnostics;
using Tritone.Dispatching;
using Tritone.Events;
using Tritone.Flows;
using Tritone.Kernel;
using Tritone.Models;
using Tritone.Tweening;
using Tritone.Unity;
using Tritone.Unity.Assets;
using Tritone.Unity.Audio;
using Tritone.Unity.Input;
using Tritone.Unity.Localization;
using Tritone.Unity.Pooling;
using Tritone.Unity.Saves;
using Tritone.Unity.Settings;
using Tritone.Unity.Tables;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AfterHours.Bootstrap
{
    /// <summary>Starts the feature selected by this scene and hosts its interactive guide.</summary>
    public sealed class AfterHoursFocusedLabBootstrap : TritoneBootstrap
    {
        [SerializeField] private string mFeatureId;

        private AfterHoursFeatureLabController mController;
        private FocusedLabModule mDemo;
        private bool mBusy;

        public string FeatureId => mFeatureId;

        protected override void Configure(GameApplicationBuilder builder)
        {
            SampleInputSource input = mFeatureId == "input" ? new SampleInputSource() : null;
            switch (mFeatureId)
            {
                case "diagnostics-logging":
                    builder.UseRuntimeDiagnostics(ELogLevel.Debug, 32);
                    break;
                case "main-thread-dispatch":
                    builder.UseMainThreadDispatcher();
                    break;
                case "flows-transitions":
                    builder.AddFlow<ReadyFlow>().AddFlow<RunningFlow>();
                    break;
                case "models-state":
                    builder.AddApplicationModel<CounterModel>();
                    break;
                case "scene-modules-switching":
                    builder.AddSceneModule<LobbyModule>().AddSceneModule<RoundModule>()
                           .UseInitialSceneModule<LobbyModule>();
                    break;
                case "tweens":
                    builder.UseTweens();
                    break;
                case "pooling-ownership":
                    builder.UsePools();
                    break;
                case "resources-provider":
                    builder.UseAssets();
                    break;
                case "input":
                    builder.UseInput(input);
                    break;
                case "audio":
                    builder.UseAssets().UseAudio();
                    break;
                case "saves":
                    builder.UseSaves();
                    break;
                case "settings":
                    builder.UseSaves().UseSettings("tritone-sample-settings");
                    break;
                case "localization":
                    builder.UseAssets().UseTables()
                           .UseLocalization("en-US", language => $"Localization/{language}");
                    break;
            }

            mDemo = new FocusedLabModule(mFeatureId, ShowResult, ShowProgress, input);
            builder.AddModule(mDemo);
        }

        protected override void Awake()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("After Hours Event System", typeof(EventSystem), typeof(StandaloneInputModule));

            base.Awake();
            mController = new AfterHoursFeatureLabController(mFeatureId, RunDemo, ResetDemo);
            mController.Show();
        }

        private async void RunDemo()
        {
            if (mBusy)
                return;
            mBusy = true;
            try
            {
                await mDemo.RunAsync();
            }
            catch (Exception exception)
            {
                ShowResult($"Could not run the demonstration.\n{exception.Message}");
            }
            finally
            {
                mBusy = false;
            }
        }

        private async void ResetDemo()
        {
            if (mBusy)
                return;
            mBusy = true;
            try
            {
                await mDemo.ResetAsync();
                mController?.ResetResult();
            }
            catch (Exception exception)
            {
                ShowResult($"Could not reset the demonstration.\n{exception.Message}");
            }
            finally
            {
                mBusy = false;
            }
        }

        private void ShowResult(string result) => mController?.SetResult(result);
        private void ShowProgress(float progress) => mController?.SetProgress(progress);

        protected override void OnDestroy()
        {
            mController?.Dispose();
            mController = null;
            base.OnDestroy();
        }

        /// <summary>Owns the API calls and state exercised by a focused demo.</summary>
        private sealed class FocusedLabModule : ModuleBase
        {
            private const string SaveSlot = "tritone-sample-counter";
            private const string SettingKey = "sample-level";

            private readonly string mFeature;
            private readonly Action<string> mReport;
            private readonly Action<float> mProgress;
            private readonly SampleInputSource mInput;
            private readonly Event<int> mSignal = new();
            private ModuleScope mListenerScope;
            private TweenHandle mTween;
            private DispatchHandle mDispatch;
            private int mCount;
            private bool mAlternate;
            private AudioHandle mSound;

            internal FocusedLabModule(string feature, Action<string> report, Action<float> progress,
                                      SampleInputSource input)
            {
                mFeature  = feature;
                mReport   = report;
                mProgress = progress;
                mInput    = input;
            }

            protected override void OnStart()
            {
                if (mFeature == "events")
                    BindEvent(mSignal, value => mReport($"Listener received signal #{value}."));
                if (mFeature == "input")
                    BindInput("Interact", () => mReport($"Interact received #{++mCount}. Tritone dispatched the named input action."));
                if (mFeature == "module-contexts-scopes")
                    AttachListener();
            }

            private void AttachListener()
            {
                mListenerScope = new ModuleScope();
                mListenerScope.Own(mSignal.Bind(value =>
                    mReport($"Listener received signal #{value}. Click again to dispose its scope.")));
            }

            internal async Task RunAsync()
            {
                switch (mFeature)
                {
                    case "application-composition":
                        var application = Context.Services.GetRequired<GameApplication>();
                        mReport($"Unity bootstrap → GameApplication → {GetType().Name}\nApplication state: {application.State} · Module action #{++mCount}");
                        break;
                    case "module-contexts-scopes":
                        if (mListenerScope != null && mCount > 0)
                        {
                            mListenerScope.Dispose();
                            mListenerScope = null;
                            mSignal.Publish(++mCount);
                            mReport($"Published signal #{mCount} after disposing the binding. No listener received it.");
                        }
                        else
                        {
                            mSignal.Publish(++mCount);
                            if (mListenerScope == null)
                                mReport("The binding is still disposed. Reset to attach a new listener.");
                        }
                        break;
                    case "flows-transitions":
                        mAlternate = !mAlternate;
                        if (mAlternate)
                            await SwitchFlowAsync<RunningFlow>();
                        else
                            await SwitchFlowAsync<ReadyFlow>();
                        mReport(mAlternate ? "Active flow: Running. Click again to enter Ready." : "Active flow: Ready. Running has exited.");
                        break;
                    case "models-state":
                        var model = GetModel<CounterModel>();
                        mReport($"Shared model count: {++model.Count}. The module reads and updates the same application-owned instance.");
                        break;
                    case "diagnostics-logging":
                        var diagnostics = Context.Services.GetRequired<IRuntimeDiagnosticsService>();
                        Logger.Info($"Sample diagnostic event #{++mCount}");
                        mReport($"Recorded event #{mCount} · Buffered logs: {diagnostics.Logs.Count}\nSampled frame: {diagnostics.Snapshot.FrameIndex}");
                        break;
                    case "main-thread-dispatch":
                        if (IsMainThreadDispatchPending(mDispatch))
                            break;
                        mReport("Callback queued. Waiting for Tritone's next update.");
                        mDispatch = PostToMainThread(() => mReport($"Callback #{++mCount} ran on Unity thread {Environment.CurrentManagedThreadId}."));
                        break;
                    case "events":
                        mSignal.Publish(++mCount);
                        break;
                    case "tweens":
                        CancelTween(mTween);
                        mTween = Tween(0f, 1f, 1.25, value =>
                        {
                            mProgress(value);
                            mReport($"Tritone interpolated the value to {value:P0}.");
                        });
                        break;
                    case "scene-modules-switching":
                        mAlternate = !mAlternate;
                        if (mAlternate)
                            SwitchModule<RoundModule>();
                        else
                            SwitchModule<LobbyModule>();
                        mReport(mAlternate ? "Lobby stopped → Round started." : "Round stopped → Lobby started.");
                        break;
                    case "input":
                        mInput.PressInteract();
                        break;
                    case "audio":
                        if (mCount > 0)
                            StopSound(mSound);
                        mSound = PlaySound("Audio/TritoneSampleChime");
                        mReport($"Playing chime #{++mCount} through Tritone's sound channel.");
                        break;
                    case "saves":
                        TryLoadSave<CounterSave>(SaveSlot, out var save);
                        save ??= new CounterSave();
                        save.Count++;
                        Save(SaveSlot, save);
                        mReport($"Saved and reloaded counter: {LoadSave<CounterSave>(SaveSlot).Count}. Re-enter this scene to continue counting.");
                        break;
                    case "settings":
                        int level = Settings.GetInt(SettingKey);
                        Settings.SetInt(SettingKey, (level + 1) % 4);
                        mReport($"Stored sample setting: {level} → {Settings.GetInt(SettingKey)}. Values cycle from 0 to 3.");
                        break;
                    case "localization":
                        mAlternate = !mAlternate;
                        await SetLanguageAsync(mAlternate ? "ja-JP" : "en-US");
                        mReport($"office.open  ·  {(mAlternate ? "ja-JP" : "en-US")}\n{Localize("office.open")}");
                        break;
                    case "pooling-ownership":
                        var first = Rent<PooledMarker>();
                        Return(first);
                        var second = Rent<PooledMarker>();
                        mReport($"Rent → return → rent again. Same object instance: {ReferenceEquals(first, second)}.");
                        Return(second);
                        break;
                    case "resources-provider":
                        var asset = await LoadAssetAsync<TextAsset>("Tables/clues");
                        if (asset == null)
                            throw new InvalidOperationException("The clue text asset could not be loaded.");
                        var size = asset.text.Length;
                        ReleaseAsset(asset);
                        mReport($"Resources loaded {size} characters from Tables/clues. The scope released its reference.");
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown feature: {mFeature}");
                }
            }

            internal async Task ResetAsync()
            {
                switch (mFeature)
                {
                    case "module-contexts-scopes":
                        mListenerScope?.Dispose();
                        AttachListener();
                        break;
                    case "flows-transitions": ExitFlow(); break;
                    case "models-state": ResetModel<CounterModel>(); break;
                    case "main-thread-dispatch": CancelMainThreadDispatch(mDispatch); break;
                    case "tweens": CancelTween(mTween); break;
                    case "scene-modules-switching": SwitchModule<LobbyModule>(); break;
                    case "audio": if (mCount > 0) StopSound(mSound); break;
                    case "saves": DeleteSave(SaveSlot); break;
                    case "settings": Settings.SetInt(SettingKey, 0); break;
                    case "localization": await SetLanguageAsync("en-US"); break;
                }

                mCount = 0;
                mAlternate = false;
            }

            protected override void OnStop() => mListenerScope?.Dispose();
        }

        /// <summary>Maps the scene's button and optional keyboard input to one Tritone action.</summary>
        private sealed class SampleInputSource : IInputSource
        {
            private bool mInteractPressed;

            internal void PressInteract() => mInteractPressed = true;

            public bool GetButtonDown(string action)
            {
                if (action != "Interact")
                    return false;

                bool pressed = mInteractPressed;
                mInteractPressed = false;
#if ENABLE_LEGACY_INPUT_MANAGER
                return pressed || Input.GetKeyDown(KeyCode.E);
#else
                return pressed;
#endif
            }

            public float GetAxis(string action) => 0f;
        }

        /// <summary>Stores shared state that Tritone can reset independently of the UI.</summary>
        private sealed class CounterModel : IModel
        {
            internal int Count;
            public void Initialize() => Reset();
            public void Reset() => Count = 0;
            public void Dispose() { }
        }

        /// <summary>Provides the ready state in the flow transition demo.</summary>
        private class ReadyFlow : IFlow
        {
            public Task EnterAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public void Update(in FrameTime time) { }
            public void Exit() { }
            public void Dispose() { }
        }

        /// <summary>Provides the running state in the flow transition demo.</summary>
        private sealed class RunningFlow : ReadyFlow { }

        /// <summary>Represents the initial scene module in the switching demo.</summary>
        private sealed class LobbyModule : ModuleBase { }

        /// <summary>Represents the second scene module in the switching demo.</summary>
        private sealed class RoundModule : ModuleBase { }

        /// <summary>Provides a reference type to rent and return in the pooling demo.</summary>
        private sealed class PooledMarker { }

        /// <summary>Stores one value in the sample's isolated save slot.</summary>
        [Serializable]
        private sealed class CounterSave
        {
            public int Count;
        }
    }
}
