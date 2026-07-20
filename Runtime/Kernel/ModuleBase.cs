using System;
using System.Threading;
using System.Threading.Tasks;
using Tritone.Audio;
using Tritone.Content;
using Tritone.Events;
using Tritone.Networking;
using Tritone.Models;
using Tritone.Settings;
using Tritone.Tables;
using Tritone.Timing;
using Tritone.UI;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides deterministic module lifecycle and compatibility facades over composed capabilities.
    /// </summary>
    public abstract class ModuleBase : IModule
    {
        // Stores the independently owned context composed for this module.
        private ModuleContext mContext;

        /// <summary>
        /// Gets the minimum severity accepted by this module.
        /// </summary>
        protected virtual ELogLevel LogLevel => ELogLevel.Info;

        /// <summary>
        /// Gets the logger automatically bound to the concrete module type.
        /// </summary>
        protected IModuleLogger Logger { get; private set; } = NullModuleLogger.Instance;

        /// <summary>
        /// Gets the composed capabilities and ownership scope for this active module.
        /// </summary>
        protected ModuleContext Context =>
            mContext ??
            throw new InvalidOperationException(
                "Module capabilities can only be used during an active lifecycle.");

        /// <summary>
        /// Gets the configured typed settings service.
        /// </summary>
        protected ISettingsService Settings => Context.Settings.Service;

        /// <summary>
        /// Gets or lazily creates one registered shared state model.
        /// </summary>
        /// <typeparam name="TModel">The concrete registered model type.</typeparam>
        /// <returns>The shared model instance for its configured lifetime.</returns>
        protected TModel GetModel<TModel>() where TModel : class, IModel
        {
            return Context.Models.Get<TModel>();
        }

        /// <summary>
        /// Resets one created shared state model.
        /// </summary>
        /// <typeparam name="TModel">The concrete registered model type.</typeparam>
        /// <returns>True when the model had been created and was reset; otherwise, false.</returns>
        protected bool ResetModel<TModel>() where TModel : class, IModel
        {
            return Context.Models.Reset<TModel>();
        }

        /// <summary>
        /// Creates the module logger and invokes module-specific configuration.
        /// </summary>
        /// <param name="context">The independently owned module context.</param>
        public void Configure(ModuleContext context)
        {
            mContext = context ?? throw new ArgumentNullException(nameof(context));
            Logger   = mContext.Activate(GetType(), LogLevel);
            try
            {
                OnConfigure(mContext.Services);
            }
            catch
            {
                ReleaseContext();
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
        /// Invokes module-specific shutdown and releases every capability-owned resource.
        /// </summary>
        public void Stop()
        {
            try
            {
                OnStop();
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// Gets localized text or returns its key when missing.
        /// </summary>
        protected string Localize(string key)
        {
            return Context.Localization.Get(key);
        }

        /// <summary>
        /// Formats localized text with supplied arguments.
        /// </summary>
        protected string Localize(string key, params object[] arguments)
        {
            return Context.Localization.Format(key, arguments);
        }

        /// <summary>
        /// Loads and activates one localization language asynchronously.
        /// </summary>
        protected Task SetLanguageAsync(string language)
        {
            return Context.Localization.SetLanguageAsync(language);
        }

        /// <summary>
        /// Binds one named button-down callback for this module lifetime.
        /// </summary>
        protected void BindInput(string action, Action callback)
        {
            Context.Input.Bind(action, callback);
        }

        /// <summary>
        /// Binds one named axis callback for this module lifetime.
        /// </summary>
        protected void BindInputAxis(string action,
                                     Action<float> callback,
                                     float deadZone = 0.001f)
        {
            Context.Input.BindAxis(action, callback, deadZone);
        }

        /// <summary>
        /// Binds one typed network message for this module lifetime.
        /// </summary>
        protected void BindMessage<T>(Action<T> callback) where T : class
        {
            Context.Network.Bind(callback);
        }

        /// <summary>
        /// Binds network state changes for this module lifetime.
        /// </summary>
        protected void BindNetworkState(Action<ENetworkState> callback)
        {
            Context.Network.BindState(callback);
        }

        /// <summary>
        /// Sends one registered typed network message.
        /// </summary>
        protected Task SendMessageAsync<T>(T message) where T : class
        {
            return Context.Network.SendAsync(message);
        }

        /// <summary>
        /// Sends one request and awaits its correlated response.
        /// </summary>
        protected Task<TResponse> RequestAsync<TRequest, TResponse>(
            TRequest request,
            double timeoutSeconds = 10.0,
            CancellationToken cancellationToken = default)
            where TRequest : class, INetworkRequest
            where TResponse : class, INetworkResponse
        {
            return Context.Network.RequestAsync<TRequest, TResponse>(
                request,
                TimeSpan.FromSeconds(timeoutSeconds),
                cancellationToken);
        }

        /// <summary>
        /// Sends one generated request and infers its declared response type.
        /// </summary>
        protected Task<TResponse> RequestAsync<TResponse>(
            INetworkRequest<TResponse> request,
            double timeoutSeconds = 10.0,
            CancellationToken cancellationToken = default)
            where TResponse : class, INetworkResponse
        {
            return Context.Network.RequestAsync(
                request,
                TimeSpan.FromSeconds(timeoutSeconds),
                cancellationToken);
        }

        /// <summary>
        /// Connects the configured network transport.
        /// </summary>
        protected Task ConnectNetworkAsync(string host, int port)
        {
            return Context.Network.ConnectAsync(host, port);
        }

        /// <summary>
        /// Disconnects the configured network transport.
        /// </summary>
        protected Task DisconnectNetworkAsync()
        {
            return Context.Network.DisconnectAsync();
        }

        /// <summary>
        /// Schedules one module-owned timer.
        /// </summary>
        protected TimerHandle SetTimer(TimerKey key,
                                       double delay,
                                       Action callback,
                                       ETimerTimeMode timeMode = ETimerTimeMode.Scaled)
        {
            return Context.Timers.Set(key, delay, callback, timeMode);
        }

        /// <summary>
        /// Schedules one repeated module-owned timer.
        /// </summary>
        protected TimerHandle SetRepeatedTimer(
            TimerKey key,
            double interval,
            Action callback,
            ETimerTimeMode timeMode = ETimerTimeMode.Scaled)
        {
            return Context.Timers.SetRepeated(key, interval, callback, timeMode);
        }

        /// <summary>
        /// Cancels one module-owned timer.
        /// </summary>
        protected bool CancelTimer(TimerKey key)
        {
            return Context.Timers.Cancel(key);
        }

        /// <summary>
        /// Determines whether one module-owned timer is active.
        /// </summary>
        protected bool IsTimerActive(TimerKey key)
        {
            return Context.Timers.IsActive(key);
        }

        /// <summary>
        /// Cancels every module-owned timer.
        /// </summary>
        protected void CancelAllTimers()
        {
            Context.Timers.CancelAll();
        }

        /// <summary>
        /// Binds one parameterless event for this module lifetime.
        /// </summary>
        protected void BindEvent(Event eventSource, Action listener)
        {
            Context.Events.Bind(eventSource, listener);
        }

        /// <summary>
        /// Binds one typed event for this module lifetime.
        /// </summary>
        protected void BindEvent<T1>(Event<T1> eventSource, Action<T1> listener)
        {
            Context.Events.Bind(eventSource, listener);
        }

        /// <summary>
        /// Binds one two-value event for this module lifetime.
        /// </summary>
        protected void BindEvent<T1, T2>(Event<T1, T2> eventSource,
                                         Action<T1, T2> listener)
        {
            Context.Events.Bind(eventSource, listener);
        }

        /// <summary>
        /// Binds one three-value event for this module lifetime.
        /// </summary>
        protected void BindEvent<T1, T2, T3>(Event<T1, T2, T3> eventSource,
                                             Action<T1, T2, T3> listener)
        {
            Context.Events.Bind(eventSource, listener);
        }

        /// <summary>
        /// Binds one four-value event for this module lifetime.
        /// </summary>
        protected void BindEvent<T1, T2, T3, T4>(
            Event<T1, T2, T3, T4> eventSource,
            Action<T1, T2, T3, T4> listener)
        {
            Context.Events.Bind(eventSource, listener);
        }

        /// <summary>
        /// Registers and owns one window definition.
        /// </summary>
        protected void AddWindow<TWindow>(
            string assetPath,
            EUILayer layer = EUILayer.Normal,
            EUIWindowLifetime lifetime = EUIWindowLifetime.Module)
            where TWindow : class
        {
            Context.UI.AddWindow(typeof(TWindow), assetPath, layer, lifetime);
        }

        /// <summary>
        /// Opens one registered window.
        /// </summary>
        protected TWindow OpenWindow<TWindow>() where TWindow : class
        {
            return (TWindow)Context.UI.Open(typeof(TWindow));
        }

        /// <summary>
        /// Opens one registered window asynchronously.
        /// </summary>
        protected async Task<TWindow> OpenWindowAsync<TWindow>() where TWindow : class
        {
            return (TWindow)await Context.UI.OpenAsync(typeof(TWindow));
        }

        /// <summary>
        /// Closes one registered window.
        /// </summary>
        protected bool CloseWindow<TWindow>() where TWindow : class
        {
            return Context.UI.Close(typeof(TWindow));
        }

        /// <summary>
        /// Gets one created window without opening it.
        /// </summary>
        protected TWindow GetWindow<TWindow>() where TWindow : class
        {
            return Context.UI.Get(typeof(TWindow)) as TWindow;
        }

        /// <summary>
        /// Determines whether one window is open.
        /// </summary>
        protected bool IsWindowOpen<TWindow>() where TWindow : class
        {
            return Context.UI.IsOpen(typeof(TWindow));
        }

        /// <summary>
        /// Switches to one registered scene module.
        /// </summary>
        protected TModule SwitchModule<TModule>() where TModule : class, IModule
        {
            return Context.Scenes.SwitchModule<TModule>();
        }

        /// <summary>
        /// Loads a Unity scene and switches its registered scene module.
        /// </summary>
        protected Task<TModule> SwitchSceneAsync<TModule>(
            string sceneName,
            Action<float> progress = null)
            where TModule : class, IModule
        {
            return Context.Scenes.SwitchAsync<TModule>(sceneName, progress);
        }

        /// <summary>
        /// Starts looping background music.
        /// </summary>
        protected void PlayMusic(string path)
        {
            Context.Audio.PlayMusic(path);
        }

        /// <summary>
        /// Loads and starts looping background music asynchronously.
        /// </summary>
        protected Task PlayMusicAsync(string path)
        {
            return Context.Audio.PlayMusicAsync(path);
        }

        /// <summary>
        /// Stops active background music.
        /// </summary>
        protected void StopMusic()
        {
            Context.Audio.StopMusic();
        }

        /// <summary>
        /// Plays one sound effect.
        /// </summary>
        protected AudioHandle PlaySound(string path)
        {
            return Context.Audio.PlaySound(path);
        }

        /// <summary>
        /// Loads and plays one sound effect asynchronously.
        /// </summary>
        protected Task<AudioHandle> PlaySoundAsync(string path)
        {
            return Context.Audio.PlaySoundAsync(path);
        }

        /// <summary>
        /// Stops one sound effect.
        /// </summary>
        protected bool StopSound(AudioHandle handle)
        {
            return Context.Audio.StopSound(handle);
        }

        /// <summary>
        /// Writes one strongly typed save slot.
        /// </summary>
        protected void Save<T>(string slot, T value) where T : class
        {
            Context.Saves.Save(slot, value);
        }

        /// <summary>
        /// Loads one required strongly typed save slot.
        /// </summary>
        protected T LoadSave<T>(string slot) where T : class
        {
            return Context.Saves.Load<T>(slot);
        }

        /// <summary>
        /// Attempts to load one optional strongly typed save slot.
        /// </summary>
        protected bool TryLoadSave<T>(string slot, out T value) where T : class
        {
            return Context.Saves.TryLoad(slot, out value);
        }

        /// <summary>
        /// Deletes one local save slot.
        /// </summary>
        protected bool DeleteSave(string slot)
        {
            return Context.Saves.Delete(slot);
        }

        /// <summary>
        /// Rents one plain C# object for this module lifetime.
        /// </summary>
        protected T Rent<T>() where T : class, new()
        {
            return Context.Pools.Rent<T>();
        }

        /// <summary>
        /// Returns one rented object.
        /// </summary>
        protected bool Return<T>(T instance) where T : class
        {
            return Context.Pools.Return(instance);
        }

        /// <summary>
        /// Returns one rented object and clears its caller reference.
        /// </summary>
        protected bool Return<T>(ref T instance) where T : class
        {
            if (instance == null || !Return(instance))
                return false;
            instance = null;
            return true;
        }

        /// <summary>
        /// Spawns one prefab for this module lifetime.
        /// </summary>
        protected T Spawn<T>(T prefab, object parent = null) where T : class
        {
            return Context.Pools.Spawn(prefab, parent);
        }

        /// <summary>
        /// Despawns one module-owned prefab instance.
        /// </summary>
        protected bool Despawn<T>(T instance) where T : class
        {
            return Context.Pools.Despawn(instance);
        }

        /// <summary>
        /// Despawns one prefab instance and clears its caller reference.
        /// </summary>
        protected bool Despawn<T>(ref T instance) where T : class
        {
            if (instance == null || !Despawn(instance))
                return false;
            instance = null;
            return true;
        }

        /// <summary>
        /// Loads and owns one asset synchronously.
        /// </summary>
        protected T LoadAsset<T>(string path) where T : class
        {
            return Context.Assets.Load<T>(path);
        }

        /// <summary>
        /// Loads and owns one asset asynchronously.
        /// </summary>
        protected Task<T> LoadAssetAsync<T>(string path) where T : class
        {
            return Context.Assets.LoadAsync<T>(path);
        }

        /// <summary>
        /// Releases one asset before this module stops.
        /// </summary>
        protected bool ReleaseAsset<T>(T asset) where T : class
        {
            return Context.Assets.Release(asset);
        }

        /// <summary>
        /// Loads and owns one strongly typed table.
        /// </summary>
        protected Table<TKey, TRow> LoadTable<TKey, TRow>(string path)
            where TRow : ITableRow<TKey>
        {
            return Context.Tables.Load<TKey, TRow>(path);
        }

        /// <summary>
        /// Loads and owns one strongly typed table asynchronously.
        /// </summary>
        protected Task<Table<TKey, TRow>> LoadTableAsync<TKey, TRow>(string path)
            where TRow : ITableRow<TKey>
        {
            return Context.Tables.LoadAsync<TKey, TRow>(path);
        }

        /// <summary>
        /// Releases one table before this module stops.
        /// </summary>
        protected bool ReleaseTable<TKey, TRow>(Table<TKey, TRow> table)
            where TRow : ITableRow<TKey>
        {
            return Context.Tables.Release(table);
        }

        /// <summary>
        /// Releases one table and clears its caller reference.
        /// </summary>
        protected bool ReleaseTable<TKey, TRow>(ref Table<TKey, TRow> table)
            where TRow : ITableRow<TKey>
        {
            if (table == null || !ReleaseTable(table))
                return false;
            table = null;
            return true;
        }

        /// <summary>
        /// Updates remote content within this module lifetime.
        /// </summary>
        protected Task<ContentUpdateResult> UpdateContentAsync(
            Action<ContentUpdateProgress> progress = null)
        {
            return Context.Content.UpdateAsync(progress);
        }

        /// <summary>
        /// Starts one callback-based content update.
        /// </summary>
        protected void StartContentUpdate(
            Action<ContentUpdateResult> completed,
            Action<ContentUpdateProgress> progress = null,
            Action<Exception> failed              = null)
        {
            Context.Content.Start(completed, progress, failed);
        }

        /// <summary>
        /// Cancels this module's active content update.
        /// </summary>
        protected void CancelContentUpdate()
        {
            Context.Content.Cancel();
        }

        /// <summary>
        /// Configures services and dependencies required by the concrete module.
        /// </summary>
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
        /// Releases this module's composed capabilities and clears retained infrastructure.
        /// </summary>
        private void ReleaseContext()
        {
            if (mContext == null)
                return;
            try
            {
                mContext.Release();
            }
            finally
            {
                mContext = null;
                Logger   = NullModuleLogger.Instance;
            }
        }
    }
}
