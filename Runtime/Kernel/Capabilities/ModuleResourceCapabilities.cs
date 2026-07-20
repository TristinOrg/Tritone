using System;
using System.Threading.Tasks;
using Tritone.Assets;
using Tritone.Content;
using Tritone.Pooling;
using Tritone.Tables;
using Tritone.UI;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides asset operations whose ownership follows one module context.
    /// </summary>
    public sealed class AssetCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific asset scope.
        private IAssetScope mScope;

        /// <summary>
        /// Initializes asset operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal AssetCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Loads and owns one asset synchronously.
        /// </summary>
        /// <typeparam name="T">The requested reference type.</typeparam>
        /// <param name="path">The provider-specific asset path.</param>
        /// <returns>The loaded asset.</returns>
        public T Load<T>(string path) where T : class
        {
            return GetScope().Load<T>(path);
        }

        /// <summary>
        /// Loads and owns one asset asynchronously.
        /// </summary>
        /// <typeparam name="T">The requested reference type.</typeparam>
        /// <param name="path">The provider-specific asset path.</param>
        /// <returns>A task containing the loaded asset.</returns>
        public Task<T> LoadAsync<T>(string path) where T : class
        {
            return GetScope().LoadAsync<T>(path);
        }

        /// <summary>
        /// Releases one asset before the module lifetime ends.
        /// </summary>
        /// <typeparam name="T">The loaded asset type.</typeparam>
        /// <param name="asset">The asset reference to release.</param>
        /// <returns>True when this capability owned the reference; otherwise, false.</returns>
        public bool Release<T>(T asset) where T : class
        {
            return mScope != null && mScope.Release(asset);
        }

        /// <summary>
        /// Gets or creates the asset scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned asset scope.</returns>
        private IAssetScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<IAssetService>(
                "Asset infrastructure is not configured. Call builder.UseAssets() before adding game modules.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }
    }

    /// <summary>
    /// Provides configuration table operations whose ownership follows one module context.
    /// </summary>
    public sealed class TableCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific table scope.
        private ITableScope mScope;

        /// <summary>
        /// Initializes table operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal TableCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Loads, indexes, and owns one strongly typed table.
        /// </summary>
        /// <typeparam name="TKey">The stable row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="path">The asset-provider table path.</param>
        /// <returns>The loaded configuration table.</returns>
        public Table<TKey, TRow> Load<TKey, TRow>(string path)
            where TRow : ITableRow<TKey>
        {
            return GetScope().Load<TKey, TRow>(path);
        }

        /// <summary>
        /// Loads, indexes, and owns one strongly typed table asynchronously.
        /// </summary>
        /// <typeparam name="TKey">The stable row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="path">The asset-provider table path.</param>
        /// <returns>A task containing the loaded configuration table.</returns>
        public Task<Table<TKey, TRow>> LoadAsync<TKey, TRow>(string path)
            where TRow : ITableRow<TKey>
        {
            return GetScope().LoadAsync<TKey, TRow>(path);
        }

        /// <summary>
        /// Releases one table before the module lifetime ends.
        /// </summary>
        /// <typeparam name="TKey">The stable row key type.</typeparam>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="table">The loaded table to release.</param>
        /// <returns>True when this capability owned the table; otherwise, false.</returns>
        public bool Release<TKey, TRow>(Table<TKey, TRow> table)
            where TRow : ITableRow<TKey>
        {
            return mScope != null && mScope.Release(table);
        }

        /// <summary>
        /// Gets or creates the table scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned table scope.</returns>
        private ITableScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<ITableService>(
                "Table infrastructure is not configured. Call builder.UseTables() before adding game modules.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }
    }

    /// <summary>
    /// Provides pooled object operations whose ownership follows one module context.
    /// </summary>
    public sealed class PoolCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific pool scope.
        private IPoolScope mScope;

        /// <summary>
        /// Initializes pool operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal PoolCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Rents and owns one plain C# object.
        /// </summary>
        /// <typeparam name="T">The pooled object type.</typeparam>
        /// <returns>The rented object.</returns>
        public T Rent<T>() where T : class, new()
        {
            return GetScope().Rent<T>();
        }

        /// <summary>
        /// Returns one rented object before the module lifetime ends.
        /// </summary>
        /// <typeparam name="T">The pooled object type.</typeparam>
        /// <param name="instance">The rented object to return.</param>
        /// <returns>True when this capability owned the object; otherwise, false.</returns>
        public bool Return<T>(T instance) where T : class
        {
            return mScope != null && mScope.Return(instance);
        }

        /// <summary>
        /// Spawns and owns one prefab instance.
        /// </summary>
        /// <typeparam name="T">The prefab reference type.</typeparam>
        /// <param name="prefab">The prefab to spawn.</param>
        /// <param name="parent">The optional Unity parent object.</param>
        /// <returns>The spawned instance.</returns>
        public T Spawn<T>(T prefab, object parent) where T : class
        {
            return GetScope().Spawn(prefab, parent);
        }

        /// <summary>
        /// Despawns one prefab instance before the module lifetime ends.
        /// </summary>
        /// <typeparam name="T">The spawned reference type.</typeparam>
        /// <param name="instance">The spawned instance to return.</param>
        /// <returns>True when this capability owned the instance; otherwise, false.</returns>
        public bool Despawn<T>(T instance) where T : class
        {
            return mScope != null && mScope.Despawn(instance);
        }

        /// <summary>
        /// Gets or creates the pool scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned pool scope.</returns>
        private IPoolScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<IPoolService>(
                "Pool infrastructure is not configured. Call builder.UsePools() before adding game modules.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }
    }

    /// <summary>
    /// Provides UI operations whose definition ownership follows one module context.
    /// </summary>
    public sealed class UICapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific window scope.
        private IUIWindowScope mScope;

        /// <summary>
        /// Initializes UI operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal UICapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Registers and owns one UI window definition.
        /// </summary>
        /// <param name="windowType">The concrete window component type.</param>
        /// <param name="assetPath">The provider path used to load its prefab.</param>
        /// <param name="layer">The visual layer receiving the window.</param>
        /// <param name="lifetime">The configured window lifetime.</param>
        public void AddWindow(Type windowType,
                              string assetPath,
                              EUILayer layer,
                              EUIWindowLifetime lifetime)
        {
            GetScope().AddWindow(windowType, assetPath, layer, lifetime);
        }

        /// <summary>
        /// Opens one registered window.
        /// </summary>
        /// <param name="windowType">The concrete registered window type.</param>
        /// <returns>The opened window instance.</returns>
        public object Open(Type windowType)
        {
            return GetService().OpenWindow(windowType);
        }

        /// <summary>
        /// Opens one registered window asynchronously.
        /// </summary>
        /// <param name="windowType">The concrete registered window type.</param>
        /// <returns>A task containing the opened window instance.</returns>
        public Task<object> OpenAsync(Type windowType)
        {
            return GetService().OpenWindowAsync(windowType);
        }

        /// <summary>
        /// Closes one registered window.
        /// </summary>
        /// <param name="windowType">The concrete registered window type.</param>
        /// <returns>True when the window was closed; otherwise, false.</returns>
        public bool Close(Type windowType)
        {
            return GetService().CloseWindow(windowType);
        }

        /// <summary>
        /// Gets one created window without opening it.
        /// </summary>
        /// <param name="windowType">The concrete registered window type.</param>
        /// <returns>The created instance, or null when unavailable.</returns>
        public object Get(Type windowType)
        {
            return GetService().GetWindow(windowType);
        }

        /// <summary>
        /// Determines whether one window is open.
        /// </summary>
        /// <param name="windowType">The concrete registered window type.</param>
        /// <returns>True when the window is open; otherwise, false.</returns>
        public bool IsOpen(Type windowType)
        {
            return GetService().IsWindowOpen(windowType);
        }

        /// <summary>
        /// Gets the configured UI service.
        /// </summary>
        /// <returns>The application UI service.</returns>
        private IUIService GetService()
        {
            return mContext.GetRequired<IUIService>(
                "UI infrastructure is not configured. Call builder.UseUI() before adding game modules.");
        }

        /// <summary>
        /// Gets or creates the UI definition scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned window scope.</returns>
        private IUIWindowScope GetScope()
        {
            if (mScope != null)
                return mScope;
            mScope = mContext.Scope.Own(GetService().CreateScope());
            return mScope;
        }
    }

    /// <summary>
    /// Provides content update operations whose cancellation follows one module context.
    /// </summary>
    public sealed class ContentCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific content update scope.
        private IContentUpdateScope mScope;

        /// <summary>
        /// Initializes content update operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal ContentCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Runs one module-owned remote content update.
        /// </summary>
        /// <param name="progress">The optional update progress callback.</param>
        /// <returns>A task containing the activated content result.</returns>
        public Task<ContentUpdateResult> UpdateAsync(
            Action<ContentUpdateProgress> progress = null)
        {
            return GetScope().UpdateAsync(progress);
        }

        /// <summary>
        /// Starts one callback-based module-owned content update.
        /// </summary>
        /// <param name="completed">The callback invoked after successful activation.</param>
        /// <param name="progress">The optional update progress callback.</param>
        /// <param name="failed">The optional failure callback.</param>
        public void Start(Action<ContentUpdateResult> completed,
                          Action<ContentUpdateProgress> progress = null,
                          Action<Exception> failed              = null)
        {
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));
            _ = RunAsync(completed, progress, failed);
        }

        /// <summary>
        /// Cancels the active module-owned content update.
        /// </summary>
        public void Cancel()
        {
            mScope?.Cancel();
        }

        /// <summary>
        /// Gets or creates the content scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned content update scope.</returns>
        private IContentUpdateScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<IContentUpdateService>(
                "Content update infrastructure is not configured. Call builder.UseContentAssets() before adding game modules.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }

        /// <summary>
        /// Observes one callback-based update and contains callback failures.
        /// </summary>
        /// <param name="completed">The success callback.</param>
        /// <param name="progress">The optional progress callback.</param>
        /// <param name="failed">The optional failure callback.</param>
        /// <returns>A task that observes the complete update operation.</returns>
        private async Task RunAsync(Action<ContentUpdateResult> completed,
                                    Action<ContentUpdateProgress> progress,
                                    Action<Exception> failed)
        {
            try
            {
                var result = await UpdateAsync(progress);
                completed.Invoke(result);
            }
            catch (OperationCanceledException)
            {
                // Module-owned cancellation is an expected lifecycle outcome.
            }
            catch (Exception exception)
            {
                if (failed == null)
                {
                    mContext.Logger.Error("Content update failed.", exception);
                    return;
                }

                try
                {
                    failed.Invoke(exception);
                }
                catch (Exception callbackException)
                {
                    mContext.Logger.Error(
                        "Content update failure callback threw an exception.",
                        callbackException);
                }
            }
        }
    }
}
