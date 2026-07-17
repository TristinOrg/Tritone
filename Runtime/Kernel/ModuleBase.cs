using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tritone.Audio;
using Tritone.Assets;
using Tritone.Content;
using Tritone.Events;
using Tritone.Pooling;
using Tritone.Scenes;
using Tritone.Tables;
using Tritone.Timing;
using Tritone.UI;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides the standard lifecycle and automatic logging used by most modules.
    /// </summary>
    public abstract class ModuleBase : IModule
    {
        /// <summary>
        /// Stores application services while this module is active.
        /// </summary>
        private IServiceRegistry mServices;

        /// <summary>
        /// Owns every timer created through this module's timer helper methods.
        /// </summary>
        private ITimerScope mTimerScope;

        /// <summary>
        /// Stores event bindings that are automatically released when this module stops.
        /// </summary>
        private List<EventBinding> mEventBindings;

        /// <summary>
        /// Owns the window types made available by this module.
        /// </summary>
        private IUIWindowScope mUIWindowScope;

        // Owns every pooled object borrowed through this module's helper methods.
        private IPoolScope mPoolScope;

        // Owns every asset reference loaded through this module's helper methods.
        private IAssetScope mAssetScope;

        // Owns every configuration table loaded through this module's helper methods.
        private ITableScope mTableScope;

        // Owns content update cancellation for this module's lifetime.
        private IContentUpdateScope mContentUpdateScope;

        /// <summary>
        /// Gets the minimum severity accepted by this module.
        /// </summary>
        protected virtual ELogLevel LogLevel => ELogLevel.Info;

        /// <summary>
        /// Gets the logger automatically bound to the concrete module type.
        /// </summary>
        protected IModuleLogger Logger { get; private set; } = NullModuleLogger.Instance;

        /// <summary>
        /// Creates the module logger and invokes module-specific configuration.
        /// </summary>
        /// <param name="context">The immutable application infrastructure available to this module.</param>
        public void Configure(ModuleContext context)
        {
            mServices = context.Services;
            Logger = context.LoggerFactory.Create(GetType(), LogLevel);
            try
            {
                OnConfigure(mServices);
            }
            catch
            {
                ReleaseTimerScope();
                UnbindAllEvents();
                ReleaseUIWindowScope();
                ReleasePoolScope();
                ReleaseTableScope();
                ReleaseAssetScope();
                ReleaseContentUpdateScope();
                mServices = null;
                Logger = NullModuleLogger.Instance;
                throw;
            }
        }

        /// <summary>
        /// Invokes module-specific startup.
        /// </summary>
        public void Start()
        {
            OnStart();
        }

        /// <summary>
        /// Invokes module-specific shutdown and releases the module logger reference.
        /// </summary>
        public void Stop()
        {
            try
            {
                OnStop();
            }
            finally
            {
                ReleaseTimerScope();
                UnbindAllEvents();
                ReleaseUIWindowScope();
                ReleasePoolScope();
                ReleaseTableScope();
                ReleaseAssetScope();
                ReleaseContentUpdateScope();
                mServices = null;
                Logger = NullModuleLogger.Instance;
            }
        }

        /// <summary>
        /// Schedules a callback owned by this module to run once after a delay.
        /// </summary>
        /// <param name="key">The caller-defined timer key.</param>
        /// <param name="delay">The non-negative delay in seconds.</param>
        /// <param name="callback">The callback invoked when the timer expires.</param>
        /// <param name="timeMode">The clock used to advance the timer.</param>
        /// <returns>A handle that can query or cancel the timer.</returns>
        protected TimerHandle SetTimer(TimerKey key,
                                       double delay,
                                       Action callback,
                                       ETimerTimeMode timeMode = ETimerTimeMode.Scaled)
        {
            return GetTimerScope().SetTimer(key, delay, callback, timeMode);
        }

        /// <summary>
        /// Schedules a callback owned by this module to run repeatedly at a fixed interval.
        /// </summary>
        /// <param name="key">The caller-defined timer key.</param>
        /// <param name="interval">The positive interval in seconds.</param>
        /// <param name="callback">The callback invoked whenever the timer expires.</param>
        /// <param name="timeMode">The clock used to advance the timer.</param>
        /// <returns>A handle that can query or cancel the timer.</returns>
        protected TimerHandle SetRepeatedTimer(TimerKey key,
                                               double interval,
                                               Action callback,
                                               ETimerTimeMode timeMode = ETimerTimeMode.Scaled)
        {
            return GetTimerScope().SetRepeatedTimer(key, interval, callback, timeMode);
        }

        /// <summary>
        /// Cancels one timer owned by this module.
        /// </summary>
        /// <param name="key">The caller-defined timer key to cancel.</param>
        /// <returns>True when an active timer was cancelled; otherwise, false.</returns>
        protected bool CancelTimer(TimerKey key)
        {
            return mTimerScope != null && mTimerScope.CancelTimer(key);
        }

        /// <summary>
        /// Determines whether this module owns an active timer.
        /// </summary>
        /// <param name="key">The caller-defined timer key to query.</param>
        /// <returns>True when the timer is active; otherwise, false.</returns>
        protected bool IsTimerActive(TimerKey key)
        {
            return mTimerScope != null && mTimerScope.IsTimerActive(key);
        }

        /// <summary>
        /// Cancels every active timer currently owned by this module.
        /// </summary>
        protected void CancelAllTimers()
        {
            mTimerScope?.CancelAllTimers();
        }

        /// <summary>
        /// Binds a parameterless event for the lifetime of this module.
        /// </summary>
        protected void BindEvent(Event eventSource, Action listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a one-parameter event for the lifetime of this module.
        /// </summary>
        protected void BindEvent<T1>(Event<T1> eventSource, Action<T1> listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a two-parameter event for the lifetime of this module.
        /// </summary>
        protected void BindEvent<T1, T2>(Event<T1, T2> eventSource, Action<T1, T2> listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a three-parameter event for the lifetime of this module.
        /// </summary>
        protected void BindEvent<T1, T2, T3>(Event<T1, T2, T3> eventSource, Action<T1, T2, T3> listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a four-parameter event for the lifetime of this module.
        /// </summary>
        protected void BindEvent<T1, T2, T3, T4>(Event<T1, T2, T3, T4> eventSource,
                                                  Action<T1, T2, T3, T4> listener)
        {
            AddEventBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Registers and owns one window without requiring a central catalog entry.
        /// </summary>
        /// <typeparam name="TWindow">The concrete window component type.</typeparam>
        /// <param name="assetPath">The provider path used to load the window prefab.</param>
        /// <param name="layer">The visual layer that receives the window.</param>
        /// <param name="lifetime">The lifetime controlling availability and release.</param>
        protected void AddWindow<TWindow>(string assetPath,
                                          EUILayer layer = EUILayer.Normal,
                                          EUIWindowLifetime lifetime = EUIWindowLifetime.Module)
            where TWindow : class
        {
            GetUIWindowScope().AddWindow(typeof(TWindow), assetPath, layer, lifetime);
        }

        /// <summary>
        /// Opens one window currently available to the application.
        /// </summary>
        /// <typeparam name="TWindow">The concrete window type.</typeparam>
        /// <returns>The opened window instance.</returns>
        protected TWindow OpenWindow<TWindow>() where TWindow : class
        {
            return (TWindow)GetUIService().OpenWindow(typeof(TWindow));
        }

        /// <summary>
        /// Opens one window asynchronously through its configured asset provider.
        /// </summary>
        /// <typeparam name="TWindow">The concrete window type.</typeparam>
        /// <returns>A task containing the opened window instance.</returns>
        protected async Task<TWindow> OpenWindowAsync<TWindow>() where TWindow : class
        {
            return (TWindow)await GetUIService().OpenWindowAsync(typeof(TWindow));
        }

        /// <summary>
        /// Closes one previously created window.
        /// </summary>
        /// <typeparam name="TWindow">The concrete window type.</typeparam>
        /// <returns>True when the window was closed; otherwise, false.</returns>
        protected bool CloseWindow<TWindow>() where TWindow : class
        {
            return GetUIService().CloseWindow(typeof(TWindow));
        }

        /// <summary>
        /// Gets one previously created window without opening it.
        /// </summary>
        /// <typeparam name="TWindow">The concrete window type.</typeparam>
        /// <returns>The window instance, or null when it has not been created.</returns>
        protected TWindow GetWindow<TWindow>() where TWindow : class
        {
            return GetUIService().GetWindow(typeof(TWindow)) as TWindow;
        }

        /// <summary>
        /// Determines whether one window is currently open.
        /// </summary>
        /// <typeparam name="TWindow">The concrete window type.</typeparam>
        /// <returns>True when the window is open; otherwise, false.</returns>
        protected bool IsWindowOpen<TWindow>() where TWindow : class
        {
            return GetUIService().IsWindowOpen(typeof(TWindow));
        }

        /// <summary>
        /// Stops the current scene module and enters a newly created registered module.
        /// </summary>
        /// <typeparam name="TModule">The concrete registered scene module type.</typeparam>
        /// <returns>The newly active module instance.</returns>
        protected TModule SwitchModule<TModule>() where TModule : class, IModule
        {
            if (mServices == null)
                throw new InvalidOperationException("Scene modules can only be switched during an active module lifecycle.");

            return (TModule)mServices.GetRequired<ISceneModuleService>().SwitchModule(typeof(TModule));
        }

        /// <summary>
        /// Loads one Unity scene before activating its registered scene module.
        /// </summary>
        /// <typeparam name="TModule">The registered scene module type.</typeparam>
        /// <param name="sceneName">The Unity scene name or path.</param>
        /// <param name="progress">The optional normalized loading progress callback.</param>
        /// <returns>A task containing the active scene module.</returns>
        protected Task<TModule> SwitchSceneAsync<TModule>(string sceneName,
                                                          Action<float> progress = null)
            where TModule : class, IModule
        {
            if (mServices == null)
                throw new InvalidOperationException(
                    "Scenes can only be switched during an active module lifecycle.");
            if (!mServices.TryGet<ISceneService>(out var sceneService))
                throw new InvalidOperationException(
                    "Scene infrastructure is not configured. Call builder.UseScenes() before adding game modules.");

            return sceneService.SwitchAsync<TModule>(sceneName, progress);
        }

        /// <summary>
        /// Starts looping background music by asset path.
        /// </summary>
        /// <param name="path">The audio clip asset path.</param>
        protected void PlayMusic(string path)
        {
            GetAudioService().PlayMusic(path);
        }

        /// <summary>
        /// Loads and starts looping background music asynchronously.
        /// </summary>
        /// <param name="path">The audio clip asset path.</param>
        /// <returns>A task completed after playback starts.</returns>
        protected Task PlayMusicAsync(string path)
        {
            return GetAudioService().PlayMusicAsync(path);
        }

        /// <summary>
        /// Stops the active background music.
        /// </summary>
        protected void StopMusic()
        {
            GetAudioService().StopMusic();
        }

        /// <summary>
        /// Plays one sound effect by asset path.
        /// </summary>
        /// <param name="path">The audio clip asset path.</param>
        /// <returns>A handle that can stop the sound.</returns>
        protected AudioHandle PlaySound(string path)
        {
            return GetAudioService().PlaySound(path);
        }

        /// <summary>
        /// Loads and plays one sound effect asynchronously.
        /// </summary>
        /// <param name="path">The audio clip asset path.</param>
        /// <returns>A task containing a handle that can stop the sound.</returns>
        protected Task<AudioHandle> PlaySoundAsync(string path)
        {
            return GetAudioService().PlaySoundAsync(path);
        }

        /// <summary>
        /// Stops one active sound effect.
        /// </summary>
        /// <param name="handle">The sound playback handle.</param>
        /// <returns>True when an active sound was stopped; otherwise, false.</returns>
        protected bool StopSound(AudioHandle handle)
        {
            return GetAudioService().StopSound(handle);
        }

        /// <summary>
        /// Rents one plain C# object from a lazily created type pool.
        /// </summary>
        protected T Rent<T>() where T : class, new()
        {
            return GetPoolScope().Rent<T>();
        }

        /// <summary>
        /// Returns one plain C# object owned by this module.
        /// </summary>
        protected bool Return<T>(T instance) where T : class
        {
            return mPoolScope != null && mPoolScope.Return(instance);
        }

        /// <summary>
        /// Returns one plain C# object and clears the caller's reference after a successful return.
        /// </summary>
        protected bool Return<T>(ref T instance) where T : class
        {
            if (instance == null || !Return(instance))
                return false;

            instance = null;
            return true;
        }

        /// <summary>
        /// Spawns one GameObject or Component prefab from a lazily created prefab pool.
        /// </summary>
        protected T Spawn<T>(T prefab, object parent = null) where T : class
        {
            return GetPoolScope().Spawn(prefab, parent);
        }

        /// <summary>
        /// Returns one spawned Unity object owned by this module.
        /// </summary>
        protected bool Despawn<T>(T instance) where T : class
        {
            return mPoolScope != null && mPoolScope.Despawn(instance);
        }

        /// <summary>
        /// Returns one spawned Unity object and clears the caller's reference after a successful return.
        /// </summary>
        protected bool Despawn<T>(ref T instance) where T : class
        {
            if (instance == null || !Despawn(instance))
                return false;

            instance = null;
            return true;
        }

        /// <summary>
        /// Loads one asset synchronously and owns its reference for this module's lifetime.
        /// </summary>
        protected T LoadAsset<T>(string path) where T : class
        {
            return GetAssetScope().Load<T>(path);
        }

        /// <summary>
        /// Loads one asset asynchronously and owns its reference for this module's lifetime.
        /// </summary>
        protected Task<T> LoadAssetAsync<T>(string path) where T : class
        {
            return GetAssetScope().LoadAsync<T>(path);
        }

        /// <summary>
        /// Releases one asset reference owned by this module before the module stops.
        /// </summary>
        protected bool ReleaseAsset<T>(T asset) where T : class
        {
            return mAssetScope != null && mAssetScope.Release(asset);
        }

        /// <summary>
        /// Loads, indexes, and owns one strongly typed configuration table.
        /// </summary>
        /// <typeparam name="TKey">The row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="path">The asset-provider path of the configuration file.</param>
        /// <returns>The loaded and indexed table.</returns>
        protected Table<TKey, TRow> LoadTable<TKey, TRow>(string path)
            where TRow : ITableRow<TKey>
        {
            return GetTableScope().Load<TKey, TRow>(path);
        }

        /// <summary>
        /// Loads, indexes, and owns one strongly typed configuration table asynchronously.
        /// </summary>
        /// <typeparam name="TKey">The row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="path">The asset-provider path of the configuration file.</param>
        /// <returns>A task containing the loaded and indexed table.</returns>
        protected Task<Table<TKey, TRow>> LoadTableAsync<TKey, TRow>(string path)
            where TRow : ITableRow<TKey>
        {
            return GetTableScope().LoadAsync<TKey, TRow>(path);
        }

        /// <summary>
        /// Releases one table reference owned by this module before the module stops.
        /// </summary>
        /// <typeparam name="TKey">The row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="table">The loaded table to release.</param>
        /// <returns>True when this module owned and released the table; otherwise, false.</returns>
        protected bool ReleaseTable<TKey, TRow>(Table<TKey, TRow> table)
            where TRow : ITableRow<TKey>
        {
            return mTableScope != null && mTableScope.Release(table);
        }

        /// <summary>
        /// Releases one owned table and clears the caller's reference after success.
        /// </summary>
        /// <typeparam name="TKey">The row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="table">The loaded table reference to release and clear.</param>
        /// <returns>True when this module owned and released the table; otherwise, false.</returns>
        protected bool ReleaseTable<TKey, TRow>(ref Table<TKey, TRow> table)
            where TRow : ITableRow<TKey>
        {
            if (table == null || !ReleaseTable(table))
                return false;

            table = null;
            return true;
        }

        /// <summary>
        /// Checks, downloads, verifies, and activates the latest content through this module's lifetime.
        /// </summary>
        /// <param name="progress">The optional progress callback.</param>
        /// <returns>A task containing the active content manifest and executed plan.</returns>
        protected Task<ContentUpdateResult> UpdateContentAsync(
            Action<ContentUpdateProgress> progress = null)
        {
            return GetContentUpdateScope().UpdateAsync(progress);
        }

        /// <summary>
        /// Starts a callback-based content update without requiring async module lifecycle code.
        /// </summary>
        /// <param name="completed">The callback receiving the active content after success.</param>
        /// <param name="progress">The optional update progress callback.</param>
        /// <param name="failed">The optional failure callback; unhandled failures are logged.</param>
        protected void StartContentUpdate(Action<ContentUpdateResult> completed,
                                          Action<ContentUpdateProgress> progress = null,
                                          Action<Exception> failed              = null)
        {
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));

            _ = RunContentUpdateAsync(completed, progress, failed);
        }

        /// <summary>
        /// Cancels the active content update owned by this module.
        /// </summary>
        protected void CancelContentUpdate()
        {
            mContentUpdateScope?.Cancel();
        }

        /// <summary>
        /// Configures services and dependencies required by the concrete module.
        /// </summary>
        /// <param name="services">The application-scoped service registry.</param>
        protected virtual void OnConfigure(IServiceRegistry services) { }

        /// <summary>
        /// Starts the concrete module.
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// Stops the concrete module.
        /// </summary>
        protected virtual void OnStop() { }

        /// <summary>
        /// Gets or lazily creates the timer scope owned by this module.
        /// </summary>
        /// <returns>The timer scope owned by this module.</returns>
        private ITimerScope GetTimerScope()
        {
            if (mTimerScope != null)
                return mTimerScope;
            if (mServices == null)
                throw new InvalidOperationException("Timers can only be created during an active module lifecycle.");
            if (!mServices.TryGet<ITimerService>(out var timerService))
                throw new InvalidOperationException("Timer infrastructure is not configured. Call builder.UseTimers() before adding game modules.");

            mTimerScope = timerService.CreateScope();
            return mTimerScope;
        }

        /// <summary>
        /// Cancels all module timers and releases the timer scope.
        /// </summary>
        private void ReleaseTimerScope()
        {
            if (mTimerScope == null)
                return;

            mTimerScope.Dispose();
            mTimerScope = null;
        }

        /// <summary>
        /// Stores one binding for automatic module lifetime cleanup.
        /// </summary>
        /// <param name="binding">The newly created event binding.</param>
        private void AddEventBinding(EventBinding binding)
        {
            mEventBindings ??= new();
            mEventBindings.Add(binding);
        }

        /// <summary>
        /// Releases every event listener owned by this module.
        /// </summary>
        private void UnbindAllEvents()
        {
            if (mEventBindings == null)
                return;

            for (int i = mEventBindings.Count - 1; i >= 0; i--)
                mEventBindings[i].Dispose();
            mEventBindings.Clear();
        }

        /// <summary>
        /// Gets the configured UI service.
        /// </summary>
        /// <returns>The application UI service.</returns>
        private IUIService GetUIService()
        {
            if (mServices == null)
                throw new InvalidOperationException("Windows can only be accessed during an active module lifecycle.");
            if (!mServices.TryGet<IUIService>(out var uiService))
                throw new InvalidOperationException("UI infrastructure is not configured. Call builder.UseUI() before adding game modules.");

            return uiService;
        }

        /// <summary>
        /// Gets or lazily creates the window scope owned by this module.
        /// </summary>
        /// <returns>The module-owned window scope.</returns>
        private IUIWindowScope GetUIWindowScope()
        {
            mUIWindowScope ??= GetUIService().CreateScope();
            return mUIWindowScope;
        }

        /// <summary>
        /// Removes this module's window ownership and releases its scope.
        /// </summary>
        private void ReleaseUIWindowScope()
        {
            if (mUIWindowScope == null)
                return;

            mUIWindowScope.Dispose();
            mUIWindowScope = null;
        }

        /// <summary>
        /// Gets or lazily creates the pool scope owned by this module.
        /// </summary>
        private IPoolScope GetPoolScope()
        {
            if (mPoolScope != null)
                return mPoolScope;
            if (mServices == null)
                throw new InvalidOperationException("Pools can only be used during an active module lifecycle.");
            if (!mServices.TryGet<IPoolService>(out var poolService))
                throw new InvalidOperationException("Pool infrastructure is not configured. Call builder.UsePools() before adding game modules.");

            mPoolScope = poolService.CreateScope();
            return mPoolScope;
        }

        /// <summary>
        /// Gets the configured shared audio service.
        /// </summary>
        /// <returns>The application audio service.</returns>
        private IAudioService GetAudioService()
        {
            if (mServices == null)
                throw new InvalidOperationException(
                    "Audio can only be used during an active module lifecycle.");
            if (!mServices.TryGet<IAudioService>(out var audioService))
                throw new InvalidOperationException(
                    "Audio infrastructure is not configured. Call builder.UseAudio() before adding game modules.");
            return audioService;
        }

        /// <summary>
        /// Returns every pooled object owned by this module and releases its scope.
        /// </summary>
        private void ReleasePoolScope()
        {
            if (mPoolScope == null)
                return;

            mPoolScope.Dispose();
            mPoolScope = null;
        }

        /// <summary>
        /// Gets or lazily creates the asset scope owned by this module.
        /// </summary>
        private IAssetScope GetAssetScope()
        {
            if (mAssetScope != null)
                return mAssetScope;
            if (mServices == null)
                throw new InvalidOperationException("Assets can only be loaded during an active module lifecycle.");
            if (!mServices.TryGet<IAssetService>(out var assetService))
                throw new InvalidOperationException("Asset infrastructure is not configured. Call builder.UseAssets() before adding game modules.");

            mAssetScope = assetService.CreateScope();
            return mAssetScope;
        }

        /// <summary>
        /// Releases every asset reference owned by this module and releases its scope.
        /// </summary>
        private void ReleaseAssetScope()
        {
            if (mAssetScope == null)
                return;

            mAssetScope.Dispose();
            mAssetScope = null;
        }

        /// <summary>
        /// Gets or lazily creates the table scope owned by this module.
        /// </summary>
        /// <returns>The module-owned table scope.</returns>
        private ITableScope GetTableScope()
        {
            if (mTableScope != null)
                return mTableScope;
            if (mServices == null)
                throw new InvalidOperationException(
                    "Tables can only be loaded during an active module lifecycle.");
            if (!mServices.TryGet<ITableService>(out var tableService))
                throw new InvalidOperationException(
                    "Table infrastructure is not configured. Call builder.UseTables() before adding game modules.");

            mTableScope = tableService.CreateScope();
            return mTableScope;
        }

        /// <summary>
        /// Releases every table reference owned by this module and its scope.
        /// </summary>
        private void ReleaseTableScope()
        {
            if (mTableScope == null)
                return;

            mTableScope.Dispose();
            mTableScope = null;
        }

        /// <summary>
        /// Gets or lazily creates content update cancellation owned by this module.
        /// </summary>
        /// <returns>The content update scope owned by this module.</returns>
        private IContentUpdateScope GetContentUpdateScope()
        {
            if (mContentUpdateScope != null)
                return mContentUpdateScope;
            if (mServices == null)
                throw new InvalidOperationException(
                    "Content can only be updated during an active module lifecycle.");
            if (!mServices.TryGet<IContentUpdateService>(out var updateService))
                throw new InvalidOperationException(
                    "Content update infrastructure is not configured. Call builder.UseContentAssets() before adding game modules.");

            mContentUpdateScope = updateService.CreateScope();
            return mContentUpdateScope;
        }

        /// <summary>
        /// Cancels the active content update and releases its module-owned scope.
        /// </summary>
        private void ReleaseContentUpdateScope()
        {
            if (mContentUpdateScope == null)
                return;

            mContentUpdateScope.Dispose();
            mContentUpdateScope = null;
        }

        /// <summary>
        /// Awaits one callback-based content update and contains all asynchronous failures.
        /// </summary>
        /// <param name="completed">The success callback.</param>
        /// <param name="progress">The optional progress callback.</param>
        /// <param name="failed">The optional failure callback.</param>
        /// <returns>A task that always observes the update operation.</returns>
        private async Task RunContentUpdateAsync(Action<ContentUpdateResult> completed,
                                                 Action<ContentUpdateProgress> progress,
                                                 Action<Exception> failed)
        {
            try
            {
                var result = await UpdateContentAsync(progress);
                completed.Invoke(result);
            }
            catch (OperationCanceledException)
            {
                // Module-owned cancellation is an expected lifecycle outcome.
            }
            catch (Exception exception)
            {
                if (failed != null)
                {
                    try
                    {
                        failed.Invoke(exception);
                    }
                    catch (Exception callbackException)
                    {
                        Logger.Error("Content update failure callback threw an exception.",
                                     callbackException);
                    }
                }
                else
                    Logger.Error("Content update failed.", exception);
            }
        }
    }
}
