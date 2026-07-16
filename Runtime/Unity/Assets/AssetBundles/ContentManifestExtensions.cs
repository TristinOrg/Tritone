using System;
using Tritone.Content;

namespace Tritone.Unity.Assets.AssetBundles
{
    /// <summary>
    /// Connects validated content manifests to the Unity AssetBundle loading registry.
    /// </summary>
    public static class ContentManifestExtensions
    {
        /// <summary>
        /// Creates a mutable AssetBundle registry from one validated content manifest.
        /// </summary>
        /// <param name="manifest">The active local content manifest.</param>
        /// <returns>A registry ready to be passed to the AssetBundle provider.</returns>
        public static AssetBundleRegistry CreateAssetBundleRegistry(this ContentManifest manifest)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));

            AssetBundleRegistry registry = new();
            var bundles                   = manifest.Bundles;
            for (int i = 0, cnt = bundles.Count; i < cnt; i++)
            {
                var bundle       = bundles[i];
                var dependencies = bundle.Dependencies;
                string[] dependencyNames;
                if (dependencies.Count == 0)
                    dependencyNames = Array.Empty<string>();
                else
                {
                    dependencyNames = new string[dependencies.Count];
                    for (int j = 0, dependencyCount = dependencies.Count; j < dependencyCount; j++)
                        dependencyNames[j] = dependencies[j];
                }

                registry.AddBundle(bundle.Name, bundle.FileName, dependencyNames);
            }

            var assets = manifest.Assets;
            for (int i = 0, cnt = assets.Count; i < cnt; i++)
            {
                var asset = assets[i];
                registry.AddAsset(asset.Address, asset.BundleName, asset.AssetName);
            }
            return registry;
        }
    }
}
