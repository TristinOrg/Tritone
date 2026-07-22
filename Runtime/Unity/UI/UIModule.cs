using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tritone.Kernel;
using Tritone.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Loads, caches, opens, closes, and lifetime-manages dynamically registered UI windows.
    /// </summary>
    public sealed class UIModule : ModuleBase, IUIService
    {
        /// <summary>
        /// Reserves a non-overlapping sorting-order interval for every logical UI layer.
        /// </summary>
        private const int LayerOrderStride = 5000;

        /// <summary>
        /// Keeps all six layer intervals inside Unity's signed 16-bit sorting-order range.
        /// </summary>
        private const int FirstLayerOrder = -15000;

        // Maps concrete window types to their definitions and runtime state.
        private readonly Dictionary<Type, WindowRecord> mWindows = new();

        // Stores the scene UI layer root.
        private readonly UIRoot mRoot;

        /// <summary>
        /// Initializes UI management with one scene layer root.
        /// </summary>
        /// <param name="root">The scene object containing UI layer parents.</param>
        public UIModule(UIRoot root)
        {
            mRoot = root ?? throw new ArgumentNullException(nameof(root));
        }

        /// <summary>
        /// Exposes UI management to application and hot-update modules.
        /// </summary>
        /// <param name="services">The application service registry.</param>
        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton<IUIService>(this);
        }

        /// <inheritdoc />
        public IUIWindowScope CreateScope()
        {
            return new UIWindowScope(this);
        }

        /// <inheritdoc />
        public object OpenWindow(Type windowType)
        {
            var record = GetAvailableRecord(windowType);
            if (record.InstanceObject == null)
            {
                if (record.OpenOperation != null)
                    throw new InvalidOperationException($"Window {windowType.Name} is already opening asynchronously.");

                try
                {
                    var prefab            = GetWindowPrefab(record);
                    record.Instance       = CreateWindow(windowType, record.Definition, prefab, out var instanceObject);
                    record.InstanceObject = instanceObject;
                }
                catch
                {
                    ReleaseWindowPrefab(record);
                    throw;
                }
            }

            record.InstanceObject.transform.SetAsLastSibling();
            record.Instance.Open();
            RefreshSortingOrders(record.Definition.Layer);
            return record.Instance;
        }

        /// <inheritdoc />
        public async Task<object> OpenWindowAsync(Type windowType)
        {
            var record = GetAvailableRecord(windowType);
            if (record.InstanceObject == null)
            {
                var operation = record.OpenOperation;
                if (operation == null)
                {
                    operation            = new(record.Version);
                    record.OpenOperation = operation;
                    operation.Task       = CreateWindowAsync(windowType, record, operation);
                }

                await operation.Task;
            }

            record = GetAvailableRecord(windowType);
            if (record.InstanceObject == null)
                throw new InvalidOperationException($"Window {windowType.Name} was released before it finished opening.");

            record.InstanceObject.transform.SetAsLastSibling();
            record.Instance.Open();
            RefreshSortingOrders(record.Definition.Layer);
            return record.Instance;
        }

        /// <inheritdoc />
        public bool CloseWindow(Type windowType)
        {
            if (!mWindows.TryGetValue(windowType, out var record) ||
                record.InstanceObject == null)
                return false;

            record.Instance.Close();
            RefreshSortingOrders(record.Definition.Layer);
            return true;
        }

        /// <inheritdoc />
        public object GetWindow(Type windowType)
        {
            if (!mWindows.TryGetValue(windowType, out var record) ||
                record.InstanceObject == null)
                return null;

            return record.Instance;
        }

        /// <inheritdoc />
        public bool IsWindowOpen(Type windowType)
        {
            if (!mWindows.TryGetValue(windowType, out var record) ||
                record.InstanceObject == null)
                return false;

            return record.InstanceObject.activeSelf;
        }

        /// <summary>
        /// Registers one definition and adds one active module owner.
        /// </summary>
        internal void RegisterAndAcquireWindow(Type windowType,
                                               string assetPath,
                                               EUILayer layer,
                                               EUIWindowLifetime lifetime)
        {
            ValidateRegistration(windowType, assetPath, layer, lifetime);
            if (mWindows.TryGetValue(windowType, out var record))
                ValidateWindowDefinition(record, windowType, assetPath, layer, lifetime);
            else
            {
                WindowDefinition definition = new(assetPath, layer, lifetime);
                record = new(definition);
                mWindows.Add(windowType, record);
            }

            if (lifetime == EUIWindowLifetime.Module)
                record.OwnerCount++;
        }

        /// <summary>
        /// Verifies that a repeated registration matches the existing definition.
        /// </summary>
        internal void ValidateWindowDefinition(Type windowType,
                                               string assetPath,
                                               EUILayer layer,
                                               EUIWindowLifetime lifetime)
        {
            ValidateRegistration(windowType, assetPath, layer, lifetime);
            if (!mWindows.TryGetValue(windowType, out var record))
                throw new InvalidOperationException($"Window {windowType.Name} is not registered.");

            ValidateWindowDefinition(record, windowType, assetPath, layer, lifetime);
        }

        /// <summary>
        /// Removes one module owner and completely removes an unowned module definition.
        /// </summary>
        internal void ReleaseWindow(Type windowType)
        {
            var record = GetRecord(windowType);
            if (record.Definition.Lifetime == EUIWindowLifetime.Application)
                return;
            if (record.OwnerCount < 1)
                throw new InvalidOperationException($"Window {windowType.Name} has no active module owner.");

            record.OwnerCount--;
            if (record.OwnerCount > 0)
                return;

            ReleaseWindow(record);
            mWindows.Remove(windowType);
        }

        /// <summary>
        /// Releases every created window and loaded prefab when the application stops.
        /// </summary>
        protected override void OnStop()
        {
            foreach (var pair in mWindows)
                ReleaseWindow(pair.Value);
            mWindows.Clear();
        }

        /// <summary>
        /// Validates one new runtime window registration.
        /// </summary>
        private static void ValidateRegistration(Type windowType,
                                                 string assetPath,
                                                 EUILayer layer,
                                                 EUIWindowLifetime lifetime)
        {
            if (windowType == null)
                throw new ArgumentNullException(nameof(windowType));
            if (string.IsNullOrWhiteSpace(assetPath))
                throw new ArgumentException("A window asset path cannot be null, empty, or whitespace.", nameof(assetPath));
            if (layer < EUILayer.Background || layer > EUILayer.System)
                throw new ArgumentOutOfRangeException(nameof(layer));
            if (lifetime != EUIWindowLifetime.Application && lifetime != EUIWindowLifetime.Module)
                throw new ArgumentOutOfRangeException(nameof(lifetime));
            if (!typeof(MonoBehaviour).IsAssignableFrom(windowType) ||
                !typeof(IUIWindow).IsAssignableFrom(windowType))
                throw new InvalidOperationException($"Type '{windowType.FullName}' is not a concrete UIWindow component.");
        }

        /// <summary>
        /// Rejects conflicting definitions registered by separate modules.
        /// </summary>
        private static void ValidateWindowDefinition(WindowRecord record,
                                                     Type windowType,
                                                     string assetPath,
                                                     EUILayer layer,
                                                     EUIWindowLifetime lifetime)
        {
            var definition = record.Definition;
            if (!string.Equals(definition.AssetPath, assetPath, StringComparison.Ordinal) ||
                definition.Layer != layer ||
                definition.Lifetime != lifetime)
                throw new InvalidOperationException($"Window {windowType.Name} is already registered with a different path, layer, or lifetime.");
        }

        /// <summary>
        /// Loads or reuses the root prefab required by one synchronous open.
        /// </summary>
        private GameObject GetWindowPrefab(WindowRecord record)
        {
            record.Prefab ??= LoadAsset<GameObject>(record.Definition.AssetPath);
            return record.Prefab;
        }

        /// <summary>
        /// Loads one prefab and creates the single cached instance for an asynchronous open.
        /// </summary>
        private async Task<IUIWindow> CreateWindowAsync(Type windowType,
                                                        WindowRecord record,
                                                        WindowOpenOperation operation)
        {
            var ownsAsset = false;
            try
            {
                var prefab = record.Prefab;
                if (prefab == null)
                {
                    prefab    = await LoadAssetAsync<GameObject>(record.Definition.AssetPath);
                    ownsAsset = true;
                }

                if (record.Version != operation.Version)
                {
                    if (ownsAsset)
                        ReleaseAsset(prefab);
                    throw new InvalidOperationException($"Window {windowType.Name} was released while its prefab was loading.");
                }

                if (ownsAsset)
                    record.Prefab = prefab;
                record.Instance       = CreateWindow(windowType, record.Definition, prefab, out var instanceObject);
                record.InstanceObject = instanceObject;
                return record.Instance;
            }
            catch
            {
                if (record.Version == operation.Version && record.InstanceObject == null)
                    ReleaseWindowPrefab(record);
                throw;
            }
            finally
            {
                if (ReferenceEquals(record.OpenOperation, operation))
                    record.OpenOperation = null;
            }
        }

        /// <summary>
        /// Creates one window instance below its configured layer.
        /// </summary>
        private IUIWindow CreateWindow(Type windowType,
                                       WindowDefinition definition,
                                       GameObject prefab,
                                       out GameObject instanceObject)
        {
            if (prefab == null)
                throw new InvalidOperationException($"Window prefab for {windowType.Name} is null.");

            var parent   = mRoot.GetLayer(definition.Layer);
            var instance = Object.Instantiate(prefab, parent, false);
            var window   = instance.GetComponent(windowType) as IUIWindow;
            if (window == null)
            {
                UnityObjectUtility.Destroy(instance);
                throw new InvalidOperationException($"Loaded prefab {prefab.name} does not contain {windowType.Name}.");
            }

            instanceObject = instance;
            return window;
        }

        /// <summary>
        /// Assigns stable non-overlapping sorting orders to active windows in one logical layer.
        /// </summary>
        /// <param name="layer">The layer whose active windows must be reordered.</param>
        private void RefreshSortingOrders(EUILayer layer)
        {
            var parent = mRoot.GetLayer(layer);
            var order  = FirstLayerOrder + (int)layer * LayerOrderStride;
            var limit  = order + LayerOrderStride;
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (!child.gameObject.activeSelf)
                    continue;
                if (!TryGetWindowRecord(child.gameObject, layer))
                    continue;
                var view = child.GetComponent<UIView>();
                if (!view)
                    continue;
                view.ApplySortingOrder(ref order);
                if (order > limit)
                    throw new InvalidOperationException($"UI layer {layer} contains more than {LayerOrderStride} preprocessed sorting nodes.");
            }
        }

        /// <summary>
        /// Checks whether one root object is an instantiated window assigned to the requested layer.
        /// </summary>
        /// <param name="instanceObject">The candidate root object.</param>
        /// <param name="layer">The expected logical layer.</param>
        /// <returns>True when a matching window record exists; otherwise, false.</returns>
        private bool TryGetWindowRecord(GameObject instanceObject, EUILayer layer)
        {
            foreach (var pair in mWindows)
            {
                var record = pair.Value;
                if (record.Definition.Layer == layer && record.InstanceObject == instanceObject)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a registered record and verifies that its lifetime currently allows opening.
        /// </summary>
        private WindowRecord GetAvailableRecord(Type windowType)
        {
            var record = GetRecord(windowType);
            if (record.Definition.Lifetime == EUIWindowLifetime.Module && record.OwnerCount == 0)
                throw new InvalidOperationException($"Window {windowType.Name} is not registered by any active module.");
            return record;
        }

        /// <summary>
        /// Gets one required runtime window record.
        /// </summary>
        private WindowRecord GetRecord(Type windowType)
        {
            if (windowType == null)
                throw new ArgumentNullException(nameof(windowType));
            if (!mWindows.TryGetValue(windowType, out var record))
                throw new InvalidOperationException($"Window {windowType.Name} is not registered by an active module.");
            return record;
        }

        /// <summary>
        /// Invalidates pending opens, destroys the cached instance, and releases its prefab.
        /// </summary>
        private void ReleaseWindow(WindowRecord record)
        {
            record.Version++;
            record.OpenOperation = null;

            var instanceObject   = record.InstanceObject;
            record.Instance       = null;
            record.InstanceObject = null;
            if (instanceObject != null)
            {
                instanceObject.SetActive(false);
                UnityObjectUtility.Destroy(instanceObject);
            }

            ReleaseWindowPrefab(record);
        }

        /// <summary>
        /// Releases the AssetModule reference owned by one window record.
        /// </summary>
        private void ReleaseWindowPrefab(WindowRecord record)
        {
            if (record.Prefab == null)
                return;

            var prefab   = record.Prefab;
            record.Prefab = null;
            ReleaseAsset(prefab);
        }

        /// <summary>
        /// Stores one immutable dynamically registered window definition.
        /// </summary>
        private sealed class WindowDefinition
        {
            // Stores the provider path used to load the window prefab.
            internal readonly string AssetPath;

            // Stores the visual layer receiving the window instance.
            internal readonly EUILayer Layer;

            // Stores the lifetime controlling availability and release.
            internal readonly EUIWindowLifetime Lifetime;

            /// <summary>
            /// Initializes one immutable window definition.
            /// </summary>
            internal WindowDefinition(string assetPath,
                                      EUILayer layer,
                                      EUIWindowLifetime lifetime)
            {
                AssetPath = assetPath;
                Layer     = layer;
                Lifetime  = lifetime;
            }
        }

        /// <summary>
        /// Stores mutable runtime state for one window definition.
        /// </summary>
        private sealed class WindowRecord
        {
            // Stores the immutable runtime definition.
            internal readonly WindowDefinition Definition;

            // Stores the loaded provider prefab while this record owns it.
            internal GameObject Prefab;

            // Stores the cached window behavior.
            internal IUIWindow Instance;

            // Stores the Unity object used for destruction-safe null checks.
            internal GameObject InstanceObject;

            // Stores one shared asynchronous open operation.
            internal WindowOpenOperation OpenOperation;

            // Stores the number of active module owners.
            internal int OwnerCount;

            // Changes whenever pending open operations must become invalid.
            internal int Version;

            /// <summary>
            /// Initializes one empty runtime record.
            /// </summary>
            internal WindowRecord(WindowDefinition definition)
            {
                Definition = definition;
            }
        }

        /// <summary>
        /// Stores one shared asynchronous window creation operation.
        /// </summary>
        private sealed class WindowOpenOperation
        {
            // Stores the record version captured when this operation started.
            internal readonly int Version;

            // Stores the task shared by all concurrent open callers.
            internal Task<IUIWindow> Task;

            /// <summary>
            /// Initializes one operation for a specific record version.
            /// </summary>
            internal WindowOpenOperation(int version)
            {
                Version = version;
            }
        }
    }
}
