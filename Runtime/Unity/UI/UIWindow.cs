using System;
using System.Collections.Generic;
using Tritone.Assets;
using Tritone.Pooling;
using UnityEngine;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Represents a complete UI window backed by a strongly typed prefab view.
    /// </summary>
    /// <typeparam name="TView">The view component attached to the window prefab.</typeparam>
    public abstract class UIWindow<TView> : UIElement<TView>, IUIWindow, IUICompositionHost where TView : UIView
    {
        /// <summary>
        /// Stores item asset paths by concrete item type.
        /// </summary>
        private readonly Dictionary<Type, string> mItemPaths = new();

        /// <summary>
        /// Stores loaded item prefab components by concrete item type.
        /// </summary>
        private readonly Dictionary<Type, Component> mItemPrefabs = new();

        /// <summary>
        /// Stores panel registrations and cached single instances by concrete panel type.
        /// </summary>
        private readonly Dictionary<Type, UIPanelDefinition> mPanels = new();

        /// <summary>
        /// Stores the explicitly injected asset service.
        /// </summary>
        private IAssetService mCompositionAssetService;

        /// <summary>
        /// Stores the explicitly injected pool service.
        /// </summary>
        private IPoolService mCompositionPoolService;

        /// <summary>
        /// Owns lazily loaded item and panel prefab assets.
        /// </summary>
        private IAssetScope mCompositionAssetScope;

        /// <summary>
        /// Owns item and panel instances for the current window activity.
        /// </summary>
        private IPoolScope mCompositionPoolScope;

        /// <summary>
        /// Indicates whether the UI module has injected composition services.
        /// </summary>
        private bool mCompositionConfigured;

        /// <summary>
        /// Gets the GameObject that owns this window.
        /// </summary>
        public GameObject GameObject => gameObject;

        /// <summary>
        /// Activates this window and enters its binding and open stages.
        /// </summary>
        public virtual void Open()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Deactivates this window and enters its close and automatic unbinding stages.
        /// </summary>
        public virtual void Close()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Registers one reusable item prefab without loading it immediately.
        /// </summary>
        /// <typeparam name="TItem">The concrete item component type.</typeparam>
        /// <param name="assetPath">The provider path used to load the item prefab.</param>
        protected void AddItemTemplate<TItem>(string assetPath) where TItem : Component, IUIItem
        {
            ValidateAssetPath(assetPath);
            var itemType = typeof(TItem);
            if (!mItemPaths.TryAdd(itemType, assetPath))
            {
                throw new InvalidOperationException($"Item template {itemType.Name} is already registered by {GetType().Name}.");
            }
        }

        /// <summary>
        /// Creates one pooled item below the requested parent.
        /// </summary>
        /// <typeparam name="TItem">The registered concrete item component type.</typeparam>
        /// <param name="parent">The item parent.</param>
        /// <returns>The active item instance.</returns>
        protected TItem CreateItem<TItem>(Transform parent) where TItem : Component, IUIItem
        {
            if (!parent)
            {
                throw new ArgumentNullException(nameof(parent));
            }
            var prefab = GetItemPrefab<TItem>();
            var item   = GetCompositionPoolScope().Spawn(prefab, parent);
            ResolveView().AddSubView(item.View);
            return item;
        }

        /// <summary>
        /// Releases one pooled item owned by this window.
        /// </summary>
        /// <typeparam name="TItem">The concrete item component type.</typeparam>
        /// <param name="item">The item to release.</param>
        /// <returns>True when this window owned and released the item.</returns>
        protected bool ReleaseItem<TItem>(TItem item) where TItem : Component, IUIItem
        {
            if (!item)
            {
                return false;
            }
            var view     = item.View;
            var released = mCompositionPoolScope != null && mCompositionPoolScope.Despawn(item);
            if (released)
            {
                ResolveView().RemoveSubView(view);
            }
            return released;
        }

        /// <summary>
        /// Releases one pooled item and clears the caller reference after success.
        /// </summary>
        /// <typeparam name="TItem">The concrete item component type.</typeparam>
        /// <param name="item">The item reference to release and clear.</param>
        /// <returns>True when this window owned and released the item.</returns>
        protected bool ReleaseItem<TItem>(ref TItem item) where TItem : Component, IUIItem
        {
            if (!ReleaseItem(item))
            {
                return false;
            }
            item = null;
            return true;
        }

        /// <summary>
        /// Registers one lazily created single-instance panel.
        /// </summary>
        /// <typeparam name="TPanel">The concrete panel component type.</typeparam>
        /// <param name="assetPath">The provider path used to load the panel prefab.</param>
        /// <param name="parent">The default panel parent, or null to use the window root.</param>
        protected void AddPanel<TPanel>(string assetPath, Transform parent = null) where TPanel : Component, IUIPanel
        {
            ValidateAssetPath(assetPath);
            var panelType = typeof(TPanel);
            if (!mPanels.TryAdd(panelType, new UIPanelDefinition(assetPath, parent)))
            {
                throw new InvalidOperationException($"Panel {panelType.Name} is already registered by {GetType().Name}.");
            }
        }

        /// <summary>
        /// Opens one registered panel and lazily creates its single instance.
        /// </summary>
        /// <typeparam name="TPanel">The registered concrete panel component type.</typeparam>
        /// <returns>The active panel instance.</returns>
        protected TPanel OpenPanel<TPanel>() where TPanel : Component, IUIPanel
        {
            var definition = GetPanelDefinition<TPanel>();
            if (!definition.InstanceObject)
            {
                var prefab = GetPanelPrefab<TPanel>(definition);
                var parent = definition.Parent ? definition.Parent : ResolveView().transform;
                var panel  = GetCompositionPoolScope().Spawn(prefab, parent);
                definition.Instance       = panel;
                definition.InstanceObject = panel.GameObject;
            }
            var instance = (TPanel)definition.Instance;
            instance.Open();
            ResolveView().AddSubView(instance.View);
            return instance;
        }

        /// <summary>
        /// Closes one created panel without releasing its cached instance.
        /// </summary>
        /// <typeparam name="TPanel">The registered concrete panel component type.</typeparam>
        /// <returns>True when a created panel was closed.</returns>
        protected bool ClosePanel<TPanel>() where TPanel : Component, IUIPanel
        {
            var definition = GetPanelDefinition<TPanel>();
            if (!definition.InstanceObject)
            {
                return false;
            }
            ResolveView().RemoveSubView(definition.Instance.View);
            definition.Instance.Close();
            return true;
        }

        /// <summary>
        /// Clears child composition state after the base activity lifetime releases pooled objects.
        /// </summary>
        protected override void OnDisable()
        {
            try
            {
                base.OnDisable();
            }
            finally
            {
                ReleaseCompositionActivity();
            }
        }

        /// <summary>
        /// Releases composition scopes before common component resources.
        /// </summary>
        protected override void OnDestroy()
        {
            try
            {
                ReleaseComposition();
            }
            finally
            {
                base.OnDestroy();
            }
        }

        /// <inheritdoc />
        void IUICompositionHost.ConfigureComposition(IAssetService assetService, IPoolService poolService)
        {
            if (mCompositionConfigured)
            {
                throw new InvalidOperationException($"UI composition services are already configured for {GetType().Name}.");
            }
            mCompositionAssetService = assetService;
            mCompositionPoolService  = poolService;
            mCompositionConfigured   = true;
        }

        /// <inheritdoc />
        void IUICompositionHost.ReleaseCompositionActivity()
        {
            ReleaseCompositionActivity();
        }

        /// <inheritdoc />
        void IUICompositionHost.ReleaseComposition()
        {
            ReleaseComposition();
        }

        /// <summary>
        /// Gets or loads one registered item prefab component.
        /// </summary>
        /// <typeparam name="TItem">The registered concrete item component type.</typeparam>
        /// <returns>The loaded item prefab component.</returns>
        private TItem GetItemPrefab<TItem>() where TItem : Component, IUIItem
        {
            var itemType = typeof(TItem);
            if (mItemPrefabs.TryGetValue(itemType, out var cachedPrefab) && cachedPrefab)
            {
                return (TItem)cachedPrefab;
            }
            if (!mItemPaths.TryGetValue(itemType, out var assetPath))
            {
                throw new InvalidOperationException($"Item template {itemType.Name} is not registered by {GetType().Name}.");
            }
            var prefabObject = GetCompositionAssetScope().Load<GameObject>(assetPath);
            var prefab       = prefabObject.GetComponent<TItem>();
            if (!prefab)
            {
                mCompositionAssetScope.Release(prefabObject);
                throw new InvalidOperationException($"Item prefab '{assetPath}' does not contain {itemType.Name}.");
            }
            mItemPrefabs[itemType] = prefab;
            return prefab;
        }

        /// <summary>
        /// Gets one required panel registration.
        /// </summary>
        /// <typeparam name="TPanel">The registered concrete panel component type.</typeparam>
        /// <returns>The panel registration.</returns>
        private UIPanelDefinition GetPanelDefinition<TPanel>() where TPanel : Component, IUIPanel
        {
            var panelType = typeof(TPanel);
            if (!mPanels.TryGetValue(panelType, out var definition))
            {
                throw new InvalidOperationException($"Panel {panelType.Name} is not registered by {GetType().Name}.");
            }
            return definition;
        }

        /// <summary>
        /// Gets or loads one registered panel prefab component.
        /// </summary>
        /// <typeparam name="TPanel">The registered concrete panel component type.</typeparam>
        /// <param name="definition">The panel registration.</param>
        /// <returns>The loaded panel prefab component.</returns>
        private TPanel GetPanelPrefab<TPanel>(UIPanelDefinition definition) where TPanel : Component, IUIPanel
        {
            if (definition.Prefab)
            {
                return (TPanel)definition.Prefab;
            }
            var prefabObject = GetCompositionAssetScope().Load<GameObject>(definition.AssetPath);
            var prefab       = prefabObject.GetComponent<TPanel>();
            if (!prefab)
            {
                mCompositionAssetScope.Release(prefabObject);
                throw new InvalidOperationException($"Panel prefab '{definition.AssetPath}' does not contain {typeof(TPanel).Name}.");
            }
            definition.Prefab = prefab;
            return prefab;
        }

        /// <summary>
        /// Validates one registered UI asset path.
        /// </summary>
        /// <param name="assetPath">The provider path to validate.</param>
        private static void ValidateAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("A UI asset path cannot be null, empty, or whitespace.", nameof(assetPath));
            }
        }

        /// <summary>
        /// Gets or creates the asset scope used by window-local composition.
        /// </summary>
        /// <returns>The composition asset scope.</returns>
        private IAssetScope GetCompositionAssetScope()
        {
            if (mCompositionAssetScope != null)
            {
                return mCompositionAssetScope;
            }
            if (mCompositionAssetService == null)
            {
                throw new InvalidOperationException("UI item and panel assets require builder.UseAssets().");
            }
            mCompositionAssetScope = mCompositionAssetService.CreateScope();
            return mCompositionAssetScope;
        }

        /// <summary>
        /// Gets or creates the pool scope used by the current window activity.
        /// </summary>
        /// <returns>The composition pool scope.</returns>
        private IPoolScope GetCompositionPoolScope()
        {
            if (mCompositionPoolScope != null)
            {
                return mCompositionPoolScope;
            }
            if (mCompositionPoolService == null)
            {
                throw new InvalidOperationException("UI item and panel pooling requires builder.UsePools().");
            }
            mCompositionPoolScope = mCompositionPoolService.CreateScope();
            return mCompositionPoolScope;
        }

        /// <summary>
        /// Releases active composed instances while retaining loaded prefab assets.
        /// </summary>
        private void ReleaseCompositionActivity()
        {
            ResolveView().ClearSubViews();
            mCompositionPoolScope?.Dispose();
            mCompositionPoolScope = null;
            foreach (var definition in mPanels.Values)
            {
                definition.Instance       = null;
                definition.InstanceObject = null;
            }
        }

        /// <summary>
        /// Releases all composition ownership scopes.
        /// </summary>
        private void ReleaseComposition()
        {
            ReleaseCompositionActivity();
            mCompositionAssetScope?.Dispose();
            mCompositionAssetScope = null;
        }
    }
}
