using System;
using System.Collections.Generic;

namespace Tritone.Unity.Assets.AssetBundles
{
    /// <summary>
    /// Composes logical asset addresses and bundle dependencies before provider startup.
    /// </summary>
    public sealed class AssetBundleRegistry
    {
        /// <summary>
        /// Stores bundle definitions by stable logical name.
        /// </summary>
        private readonly Dictionary<string, BundleDefinition> mBundles = new(StringComparer.Ordinal);

        /// <summary>
        /// Stores asset definitions by public load address.
        /// </summary>
        private readonly Dictionary<string, AssetDefinition> mAssets = new(StringComparer.Ordinal);

        /// <summary>
        /// Indicates whether a provider has made this registry immutable.
        /// </summary>
        private bool mSealed;

        /// <summary>
        /// Adds one logical bundle and its direct dependencies.
        /// </summary>
        /// <param name="bundleName">The stable logical bundle name.</param>
        /// <param name="fileName">The file name passed to the configured bundle source.</param>
        /// <param name="dependencies">The logical names of bundles required before this bundle.</param>
        /// <returns>This registry so feature registrations can be chained.</returns>
        public AssetBundleRegistry AddBundle(string bundleName,
                                             string fileName,
                                             params string[] dependencies)
        {
            ThrowIfSealed();
            ValidateName(bundleName, nameof(bundleName));
            ValidateName(fileName, nameof(fileName));
            if (mBundles.ContainsKey(bundleName))
                throw new InvalidOperationException($"AssetBundle '{bundleName}' is already registered.");

            var dependencyCopy = dependencies == null || dependencies.Length == 0
                ? Array.Empty<string>()
                : (string[])dependencies.Clone();
            for (int i = 0, cnt = dependencyCopy.Length; i < cnt; i++)
                ValidateName(dependencyCopy[i], nameof(dependencies));

            mBundles.Add(bundleName, new BundleDefinition(bundleName, fileName, dependencyCopy));
            return this;
        }

        /// <summary>
        /// Adds one public asset address mapped to an asset inside a logical bundle.
        /// </summary>
        /// <param name="address">The address passed to LoadAsset.</param>
        /// <param name="bundleName">The logical bundle containing the asset.</param>
        /// <param name="assetName">The exact asset name passed to the bundle handle.</param>
        /// <returns>This registry so feature registrations can be chained.</returns>
        public AssetBundleRegistry AddAsset(string address,
                                            string bundleName,
                                            string assetName)
        {
            ThrowIfSealed();
            ValidateName(address, nameof(address));
            ValidateName(bundleName, nameof(bundleName));
            ValidateName(assetName, nameof(assetName));
            if (mAssets.ContainsKey(address))
                throw new InvalidOperationException($"Asset address '{address}' is already registered.");

            mAssets.Add(address, new AssetDefinition(bundleName, assetName));
            return this;
        }

        /// <summary>
        /// Validates every reference, precomputes dependency order, and seals this registry.
        /// </summary>
        internal AssetBundleRegistrySnapshot CreateSnapshot()
        {
            ThrowIfSealed();
            mSealed = true;

            Dictionary<string, string[]> loadOrders = new(mBundles.Count, StringComparer.Ordinal);
            Dictionary<string, byte> states         = new(mBundles.Count, StringComparer.Ordinal);
            List<string> path                       = new(mBundles.Count);
            foreach (var pair in mBundles)
            {
                List<string> order = new();
                BuildLoadOrder(pair.Key, states, path, order);
                loadOrders.Add(pair.Key, order.ToArray());
                states.Clear();
                path.Clear();
            }

            foreach (var pair in mAssets)
            {
                if (!mBundles.ContainsKey(pair.Value.BundleName))
                    throw new InvalidOperationException($"Asset '{pair.Key}' references missing bundle '{pair.Value.BundleName}'.");
            }

            return new AssetBundleRegistrySnapshot(mBundles, mAssets, loadOrders);
        }

        /// <summary>
        /// Recursively builds one dependency-first order and rejects dependency cycles.
        /// </summary>
        private void BuildLoadOrder(string bundleName,
                                    Dictionary<string, byte> states,
                                    List<string> path,
                                    List<string> order)
        {
            if (!mBundles.TryGetValue(bundleName, out var definition))
                throw new InvalidOperationException($"AssetBundle dependency '{bundleName}' is not registered.");
            if (states.TryGetValue(bundleName, out var state))
            {
                if (state == 2)
                    return;
                if (state == 1)
                {
                    path.Add(bundleName);
                    throw new InvalidOperationException($"AssetBundle dependency cycle detected: {string.Join(" -> ", path)}.");
                }
            }

            states[bundleName] = 1;
            path.Add(bundleName);
            for (int i = 0, cnt = definition.Dependencies.Length; i < cnt; i++)
                BuildLoadOrder(definition.Dependencies[i], states, path, order);
            path.RemoveAt(path.Count - 1);
            states[bundleName] = 2;
            order.Add(bundleName);
        }

        /// <summary>
        /// Rejects registry changes after provider construction.
        /// </summary>
        private void ThrowIfSealed()
        {
            if (mSealed)
                throw new InvalidOperationException("The AssetBundle registry is sealed and cannot be modified.");
        }

        /// <summary>
        /// Rejects missing registry identifiers.
        /// </summary>
        private static void ValidateName(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("AssetBundle registry values cannot be null, empty, or whitespace.", parameterName);
        }
    }

    /// <summary>
    /// Stores one immutable logical bundle definition.
    /// </summary>
    internal sealed class BundleDefinition
    {
        /// <summary>
        /// Stores the stable logical bundle name.
        /// </summary>
        internal readonly string BundleName;

        /// <summary>
        /// Stores the source-specific bundle file name.
        /// </summary>
        internal readonly string FileName;

        /// <summary>
        /// Stores direct logical bundle dependencies.
        /// </summary>
        internal readonly string[] Dependencies;

        /// <summary>
        /// Initializes one immutable bundle definition.
        /// </summary>
        internal BundleDefinition(string bundleName, string fileName, string[] dependencies)
        {
            BundleName   = bundleName;
            FileName     = fileName;
            Dependencies = dependencies;
        }
    }

    /// <summary>
    /// Stores one immutable logical asset definition.
    /// </summary>
    internal sealed class AssetDefinition
    {
        /// <summary>
        /// Stores the logical containing bundle name.
        /// </summary>
        internal readonly string BundleName;

        /// <summary>
        /// Stores the exact name passed to the bundle handle.
        /// </summary>
        internal readonly string AssetName;

        /// <summary>
        /// Initializes one immutable asset definition.
        /// </summary>
        internal AssetDefinition(string bundleName, string assetName)
        {
            BundleName = bundleName;
            AssetName  = assetName;
        }
    }

    /// <summary>
    /// Provides an immutable validated registry to one provider instance.
    /// </summary>
    internal sealed class AssetBundleRegistrySnapshot
    {
        /// <summary>
        /// Stores bundle definitions by logical name.
        /// </summary>
        private readonly Dictionary<string, BundleDefinition> mBundles;

        /// <summary>
        /// Stores asset definitions by public address.
        /// </summary>
        private readonly Dictionary<string, AssetDefinition> mAssets;

        /// <summary>
        /// Stores dependency-first unique bundle orders by root bundle.
        /// </summary>
        private readonly Dictionary<string, string[]> mLoadOrders;

        /// <summary>
        /// Initializes one immutable registry snapshot.
        /// </summary>
        internal AssetBundleRegistrySnapshot(Dictionary<string, BundleDefinition> bundles,
                                             Dictionary<string, AssetDefinition> assets,
                                             Dictionary<string, string[]> loadOrders)
        {
            mBundles    = new(bundles, StringComparer.Ordinal);
            mAssets     = new(assets, StringComparer.Ordinal);
            mLoadOrders = loadOrders;
        }

        /// <summary>
        /// Gets one required asset definition.
        /// </summary>
        internal AssetDefinition GetAsset(string address)
        {
            if (!mAssets.TryGetValue(address, out var definition))
                throw new InvalidOperationException($"Asset address '{address}' is not registered in the AssetBundle registry.");
            return definition;
        }

        /// <summary>
        /// Gets one required bundle definition.
        /// </summary>
        internal BundleDefinition GetBundle(string bundleName)
        {
            return mBundles[bundleName];
        }

        /// <summary>
        /// Gets the dependency-first unique order for one asset bundle.
        /// </summary>
        internal string[] GetLoadOrder(string bundleName)
        {
            return mLoadOrders[bundleName];
        }
    }
}
