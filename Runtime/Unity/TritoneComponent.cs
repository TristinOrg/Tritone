using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tritone.Assets;
using Tritone.Events;
using Tritone.Kernel;
using Tritone.Pooling;
using Tritone.UI;
using UnityEngine;

namespace Tritone.Unity
{
    /// <summary>
    /// Provides module access and automatically managed Tritone event bindings for Unity components.
    /// </summary>
    public abstract class TritoneComponent : MonoBehaviour
    {
        /// <summary>
        /// Stores bindings owned by this component.
        /// </summary>
        private List<IDisposable> mBindings;

        // Owns pooled objects borrowed by this Unity component.
        private IPoolScope mPoolScope;

        // Owns asset references loaded by this Unity component.
        private IAssetScope mAssetScope;

        /// <summary>
        /// Gets one registered application module.
        /// </summary>
        /// <typeparam name="TModule">The concrete module type to resolve.</typeparam>
        /// <returns>The registered module instance.</returns>
        protected TModule GetModule<TModule>() where TModule : class
        {
            var application = TritoneBootstrap.Current;
            if (application == null)
                throw new InvalidOperationException("No running Tritone bootstrap is available.");

            return application.Services.GetRequired<TModule>();
        }

        /// <summary>
        /// Opens one window currently available to an active module or the application.
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
        protected bool CloseWindow<TWindow>() where TWindow : class
        {
            return GetUIService().CloseWindow(typeof(TWindow));
        }

        /// <summary>
        /// Stops the current scene module and enters a newly created registered module.
        /// </summary>
        /// <typeparam name="TModule">The concrete registered scene module type.</typeparam>
        /// <returns>The newly active module instance.</returns>
        protected TModule SwitchModule<TModule>() where TModule : class, IModule
        {
            var application = TritoneBootstrap.Current;
            if (application == null)
                throw new InvalidOperationException("No running Tritone bootstrap is available.");

            return application.SwitchModule<TModule>();
        }

        /// <summary>
        /// Rents one plain C# object from a lazily created type pool.
        /// </summary>
        protected T Rent<T>() where T : class, new()
        {
            return GetPoolScope().Rent<T>();
        }

        /// <summary>
        /// Returns one plain C# object owned by this component.
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
        /// Spawns one GameObject or Component prefab below an optional parent.
        /// </summary>
        protected T Spawn<T>(T prefab, Transform parent = null) where T : UnityEngine.Object
        {
            return GetPoolScope().Spawn(prefab, parent);
        }

        /// <summary>
        /// Spawns one GameObject or Component prefab at a world transform.
        /// </summary>
        protected T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
        {
            var instance = Spawn(prefab);
            var transform = instance is GameObject gameObject
                ? gameObject.transform
                : ((Component)(UnityEngine.Object)instance).transform;
            transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        /// <summary>
        /// Returns one spawned Unity object owned by this component.
        /// </summary>
        protected bool Despawn<T>(T instance) where T : UnityEngine.Object
        {
            return mPoolScope != null && mPoolScope.Despawn(instance);
        }

        /// <summary>
        /// Returns one spawned Unity object and clears the caller's reference after a successful return.
        /// </summary>
        protected bool Despawn<T>(ref T instance) where T : UnityEngine.Object
        {
            if (instance == null || !Despawn(instance))
                return false;

            instance = null;
            return true;
        }

        /// <summary>
        /// Loads one asset synchronously and owns its reference for this component's lifetime.
        /// </summary>
        protected T LoadAsset<T>(string path) where T : class
        {
            return GetAssetScope().Load<T>(path);
        }

        /// <summary>
        /// Loads one asset asynchronously and owns its reference for this component's lifetime.
        /// </summary>
        protected Task<T> LoadAssetAsync<T>(string path) where T : class
        {
            return GetAssetScope().LoadAsync<T>(path);
        }

        /// <summary>
        /// Releases one asset reference owned by this component before it is destroyed.
        /// </summary>
        protected bool ReleaseAsset<T>(T asset) where T : class
        {
            return mAssetScope != null && mAssetScope.Release(asset);
        }

        /// <summary>
        /// Gets the configured application UI service.
        /// </summary>
        private static IUIService GetUIService()
        {
            var application = TritoneBootstrap.Current;
            if (application == null)
                throw new InvalidOperationException("No running Tritone bootstrap is available.");

            return application.Services.GetRequired<IUIService>();
        }

        /// <summary>
        /// Binds a parameterless Tritone event.
        /// </summary>
        /// <param name="eventSource">The event that owns the listener.</param>
        /// <param name="listener">The callback invoked when the event is published.</param>
        protected void BindEvent(Tritone.Events.Event eventSource, Action listener)
        {
            AddBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a one-parameter Tritone event.
        /// </summary>
        protected void BindEvent<T1>(Event<T1> eventSource, Action<T1> listener)
        {
            AddBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a two-parameter Tritone event.
        /// </summary>
        protected void BindEvent<T1, T2>(Event<T1, T2> eventSource, Action<T1, T2> listener)
        {
            AddBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a three-parameter Tritone event.
        /// </summary>
        protected void BindEvent<T1, T2, T3>(Event<T1, T2, T3> eventSource, Action<T1, T2, T3> listener)
        {
            AddBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Binds a four-parameter Tritone event.
        /// </summary>
        protected void BindEvent<T1, T2, T3, T4>(Event<T1, T2, T3, T4> eventSource,
                                                  Action<T1, T2, T3, T4> listener)
        {
            AddBinding(eventSource?.Bind(listener) ?? throw new ArgumentNullException(nameof(eventSource)));
        }

        /// <summary>
        /// Stores one disposable binding for automatic cleanup.
        /// </summary>
        /// <param name="binding">The binding owned by this component.</param>
        protected void AddBinding(IDisposable binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            mBindings ??= new();
            mBindings.Add(binding);
        }

        /// <summary>
        /// Releases every binding owned by this component in reverse registration order.
        /// </summary>
        protected void ReleaseBindings()
        {
            if (mBindings == null)
                return;

            for (int i = mBindings.Count - 1; i >= 0; i--)
                mBindings[i].Dispose();
            mBindings.Clear();
        }

        /// <summary>
        /// Returns every pooled object owned by the current component activity lifetime.
        /// </summary>
        protected void ReleasePooledObjects()
        {
            if (mPoolScope == null)
                return;

            mPoolScope.Dispose();
            mPoolScope = null;
        }

        /// <summary>
        /// Releases retained listeners when Unity destroys this component.
        /// </summary>
        protected virtual void OnDestroy()
        {
            ReleaseBindings();
            ReleasePooledObjects();
            ReleaseAssetScope();
        }

        /// <summary>
        /// Gets or lazily creates the pool scope owned by this component.
        /// </summary>
        private IPoolScope GetPoolScope()
        {
            if (mPoolScope != null)
                return mPoolScope;

            var application = TritoneBootstrap.Current;
            if (application == null)
                throw new InvalidOperationException("No running Tritone bootstrap is available.");
            mPoolScope = application.Services.GetRequired<IPoolService>().CreateScope();
            return mPoolScope;
        }

        /// <summary>
        /// Gets or lazily creates the asset scope owned by this component.
        /// </summary>
        private IAssetScope GetAssetScope()
        {
            if (mAssetScope != null)
                return mAssetScope;

            var application = TritoneBootstrap.Current;
            if (application == null)
                throw new InvalidOperationException("No running Tritone bootstrap is available.");
            mAssetScope = application.Services.GetRequired<IAssetService>().CreateScope();
            return mAssetScope;
        }

        /// <summary>
        /// Releases every asset reference owned by this component and releases its scope.
        /// </summary>
        private void ReleaseAssetScope()
        {
            if (mAssetScope == null)
                return;

            mAssetScope.Dispose();
            mAssetScope = null;
        }
    }
}
