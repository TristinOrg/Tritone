using System;
using System.IO;
using Tritone.Content;
using UnityEngine;

namespace Tritone.Unity.ContentUpdates
{
    /// <summary>
    /// Serializes immutable content manifests through Unity's allocation-conscious JSON utility.
    /// </summary>
    public sealed class UnityJsonContentManifestSerializer : IContentManifestSerializer
    {
        /// <inheritdoc />
        public ContentManifest Deserialize(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidDataException("Serialized content manifest data is empty.");

            ManifestData data;
            try
            {
                data = JsonUtility.FromJson<ManifestData>(content);
            }
            catch (Exception exception)
            {
                throw new InvalidDataException("Content manifest JSON is invalid.", exception);
            }
            if (data == null)
                throw new InvalidDataException("Content manifest JSON did not produce a manifest.");

            var sourceBundles = data.Bundles ?? Array.Empty<BundleData>();
            var bundles       = new ContentBundle[sourceBundles.Length];
            for (int i = 0, cnt = sourceBundles.Length; i < cnt; i++)
            {
                var bundle = sourceBundles[i] ??
                             throw new InvalidDataException($"Content bundle JSON entry {i} is null.");
                bundles[i] = new ContentBundle(bundle.Name,
                                               bundle.FileName,
                                               bundle.Hash,
                                               bundle.Size,
                                               bundle.Dependencies ?? Array.Empty<string>());
            }

            var sourceAssets = data.Assets ?? Array.Empty<AssetData>();
            var assets       = new ContentAsset[sourceAssets.Length];
            for (int i = 0, cnt = sourceAssets.Length; i < cnt; i++)
            {
                var asset = sourceAssets[i] ??
                            throw new InvalidDataException($"Content asset JSON entry {i} is null.");
                assets[i] = new ContentAsset(asset.Address,
                                             asset.BundleName,
                                             asset.AssetName);
            }
            return new ContentManifest(data.Version, bundles, assets);
        }

        /// <inheritdoc />
        public string Serialize(ContentManifest manifest)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));

            var bundles    = manifest.Bundles;
            var bundleData = new BundleData[bundles.Count];
            for (int i = 0, cnt = bundles.Count; i < cnt; i++)
            {
                var bundle        = bundles[i];
                var dependencies  = bundle.Dependencies;
                var dependencyData = new string[dependencies.Count];
                for (int j = 0, dependencyCount = dependencies.Count; j < dependencyCount; j++)
                    dependencyData[j] = dependencies[j];

                bundleData[i] = new BundleData
                {
                    Name         = bundle.Name,
                    FileName     = bundle.FileName,
                    Hash         = bundle.Hash,
                    Size         = bundle.Size,
                    Dependencies = dependencyData
                };
            }

            var assets    = manifest.Assets;
            var assetData = new AssetData[assets.Count];
            for (int i = 0, cnt = assets.Count; i < cnt; i++)
            {
                var asset = assets[i];
                assetData[i] = new AssetData
                {
                    Address    = asset.Address,
                    BundleName = asset.BundleName,
                    AssetName  = asset.AssetName
                };
            }

            ManifestData data = new()
            {
                Version = manifest.Version,
                Bundles = bundleData,
                Assets  = assetData
            };
            return JsonUtility.ToJson(data);
        }

        /// <summary>
        /// Stores Unity-serializable manifest transport fields.
        /// </summary>
        [Serializable]
        private sealed class ManifestData
        {
            // Stores the manifest version label.
            public string Version;

            // Stores serialized bundle definitions.
            public BundleData[] Bundles;

            // Stores serialized addressed asset definitions.
            public AssetData[] Assets;
        }

        /// <summary>
        /// Stores Unity-serializable bundle transport fields.
        /// </summary>
        [Serializable]
        private sealed class BundleData
        {
            // Stores the stable logical bundle name.
            public string Name;

            // Stores the portable relative bundle file name.
            public string FileName;

            // Stores the lowercase SHA-256 file hash.
            public string Hash;

            // Stores the expected file size in bytes.
            public long Size;

            // Stores ordered direct logical dependencies.
            public string[] Dependencies;
        }

        /// <summary>
        /// Stores Unity-serializable addressed asset transport fields.
        /// </summary>
        [Serializable]
        private sealed class AssetData
        {
            // Stores the public asset address.
            public string Address;

            // Stores the containing logical bundle name.
            public string BundleName;

            // Stores the exact asset name inside the bundle.
            public string AssetName;
        }
    }
}
