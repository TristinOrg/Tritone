using System;
using System.Collections.Generic;
using Tritone.Kernel;
using Tritone.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tritone.Unity.UI
{
    /// <summary>
    /// Creates, caches, opens, closes, and lifetime-manages catalogued UI windows.
    /// </summary>
    public sealed class UIModule : ModuleBase, IUIService
    {
        /// <summary>Maps concrete window types to their catalog and runtime state.</summary>
        private readonly Dictionary<Type, WindowRecord> mWindows = new();

        /// <summary>Stores the scene UI layer root.</summary>
        private readonly UIRoot                          mRoot;

        /// <summary>Stores the fixed project window catalog.</summary>
        private readonly UIWindowCatalog                mCatalog;

        /// <summary>
        /// Initializes UI management with one scene root and one project catalog.
        /// </summary>
        /// <param name="root">The scene object containing UI layer parents.</param>
        /// <param name="catalog">The fixed project window catalog.</param>
        public UIModule(UIRoot root, UIWindowCatalog catalog)
        {
            mRoot    = root ?? throw new ArgumentNullException(nameof(root));
            mCatalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        /// <summary>
        /// Builds the runtime lookup and exposes UI management to other modules.
        /// </summary>
        /// <param name="services">The application service registry.</param>
        protected override void OnConfigure(IServiceRegistry services)
        {
            BuildCatalog();
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
            if (record.Instance == null || record.Instance.GameObject == null)
                record.Instance = CreateWindow(windowType, record.Definition);

            record.Instance.Open();
            return record.Instance;
        }

        /// <inheritdoc />
        public bool CloseWindow(Type windowType)
        {
            if (!mWindows.TryGetValue(windowType, out var record) ||
                record.Instance == null ||
                record.Instance.GameObject == null)
                return false;

            record.Instance.Close();
            return true;
        }

        /// <inheritdoc />
        public object GetWindow(Type windowType)
        {
            if (!mWindows.TryGetValue(windowType, out var record) ||
                record.Instance == null ||
                record.Instance.GameObject == null)
                return null;

            return record.Instance;
        }

        /// <inheritdoc />
        public bool IsWindowOpen(Type windowType)
        {
            if (!mWindows.TryGetValue(windowType, out var record) ||
                record.Instance == null ||
                record.Instance.GameObject == null)
                return false;

            return record.Instance.GameObject.activeSelf;
        }

        /// <summary>
        /// Adds one active module owner to a catalogued module window.
        /// </summary>
        /// <param name="windowType">The concrete window type.</param>
        internal void AcquireWindow(Type windowType)
        {
            var record = GetRecord(windowType);
            if (record.Definition.Lifetime == EUIWindowLifetime.Application)
                return;

            record.OwnerCount++;
        }

        /// <summary>
        /// Removes one module owner and destroys an unowned module window.
        /// </summary>
        /// <param name="windowType">The concrete window type.</param>
        internal void ReleaseWindow(Type windowType)
        {
            var record = GetRecord(windowType);
            if (record.Definition.Lifetime == EUIWindowLifetime.Application)
                return;
            if (record.OwnerCount < 1)
                throw new InvalidOperationException($"Window {windowType.Name} has no active module owner.");

            record.OwnerCount--;
            if (record.OwnerCount == 0)
                DestroyWindow(record);
        }

        /// <summary>
        /// Destroys every created window and releases catalog references.
        /// </summary>
        protected override void OnStop()
        {
            foreach (var pair in mWindows)
                DestroyWindow(pair.Value);
            mWindows.Clear();
        }

        /// <summary>
        /// Validates configured prefabs and builds the type lookup once during startup.
        /// </summary>
        private void BuildCatalog()
        {
            var definitions = mCatalog.Windows ?? Array.Empty<UIWindowDefinition>();
            for (int i = 0, cnt = definitions.Length; i < cnt; i++)
            {
                var definition = definitions[i];
                if (definition == null || definition.Prefab == null)
                    throw new InvalidOperationException($"Window catalog entry {i} has no prefab.");

                var behaviours = definition.Prefab.GetComponents<MonoBehaviour>();
                Type windowType = null;
                for (int j = 0, behaviourCount = behaviours.Length; j < behaviourCount; j++)
                {
                    if (behaviours[j] is not IUIWindow)
                        continue;
                    if (windowType != null)
                        throw new InvalidOperationException($"Window prefab {definition.Prefab.name} contains multiple UIWindow components.");
                    windowType = behaviours[j].GetType();
                }

                if (windowType == null)
                    throw new InvalidOperationException($"Window prefab {definition.Prefab.name} contains no UIWindow component.");
                if (mWindows.ContainsKey(windowType))
                    throw new InvalidOperationException($"Window {windowType.Name} is registered more than once.");

                mWindows.Add(windowType, new WindowRecord(definition));
            }
        }

        /// <summary>
        /// Creates one window instance below its configured layer.
        /// </summary>
        private IUIWindow CreateWindow(Type windowType, UIWindowDefinition definition)
        {
            var parent   = mRoot.GetLayer(definition.Layer);
            var instance = Object.Instantiate(definition.Prefab, parent, false);
            var window   = instance.GetComponent(windowType) as IUIWindow;
            if (window == null)
            {
                Object.Destroy(instance);
                throw new InvalidOperationException($"Created prefab {definition.Prefab.name} does not contain {windowType.Name}.");
            }

            return window;
        }

        /// <summary>
        /// Gets a catalog record and verifies that its lifetime currently allows opening.
        /// </summary>
        private WindowRecord GetAvailableRecord(Type windowType)
        {
            var record = GetRecord(windowType);
            if (record.Definition.Lifetime == EUIWindowLifetime.Module && record.OwnerCount == 0)
                throw new InvalidOperationException($"Window {windowType.Name} is not registered by any active module.");
            return record;
        }

        /// <summary>
        /// Gets one required catalog record.
        /// </summary>
        private WindowRecord GetRecord(Type windowType)
        {
            if (windowType == null)
                throw new ArgumentNullException(nameof(windowType));
            if (!mWindows.TryGetValue(windowType, out var record))
                throw new InvalidOperationException($"Window {windowType.Name} is missing from UIWindowCatalog.");
            return record;
        }

        /// <summary>
        /// Closes and destroys one cached window instance.
        /// </summary>
        private static void DestroyWindow(WindowRecord record)
        {
            if (record.Instance == null || record.Instance.GameObject == null)
                return;

            record.Instance.Close();
            Object.Destroy(record.Instance.GameObject);
            record.Instance = null;
        }

        /// <summary>
        /// Stores mutable runtime state for one immutable catalog definition.
        /// </summary>
        private sealed class WindowRecord
        {
            internal readonly UIWindowDefinition Definition;
            internal IUIWindow Instance;
            internal int       OwnerCount;

            internal WindowRecord(UIWindowDefinition definition)
            {
                Definition = definition;
            }
        }
    }
}
