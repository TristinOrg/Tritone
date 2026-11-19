#if UNITY_EDITOR
using System.IO;
using Tritone.Editor.ContentBuild;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;

namespace AfterHours.Editor
{
    /// <summary>Prepares the sample's provider labs and builds local bundle content.</summary>
    public static class AfterHoursContentAuthoring
    {
        internal const string ClueTablePath = "Assets/AfterHours/Runtime/Resources/Tables/clues.json";
        internal const string SystemsPanelPath = "Assets/AfterHours/Runtime/Resources/UI/SystemsPanel.prefab";
        private const string BundleName = "after-hours-case-data";

        [MenuItem("Tritone/After Hours/Prepare Provider Labs")]
        public static void PrepareProviderLabs()
        {
            if (!File.Exists(ClueTablePath) || !File.Exists(SystemsPanelPath))
                throw new FileNotFoundException("Import the After Hours sample before preparing provider labs.", ClueTablePath);

            var importer = AssetImporter.GetAtPath(ClueTablePath);
            importer.assetBundleName = BundleName;
            importer.SaveAndReimport();
            var prefabImporter = AssetImporter.GetAtPath(SystemsPanelPath);
            prefabImporter.assetBundleName = BundleName;
            prefabImporter.SaveAndReimport();

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.FindGroup("After Hours Local Content") ??
                        settings.CreateGroup("After Hours Local Content", false, false, true, null,
                            typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
            string[] paths = { ClueTablePath, SystemsPanelPath };
            string[] addresses = { "Tables/clues", "UI/SystemsPanel" };
            for (int i = 0; i < paths.Length; i++)
            {
                var guid = AssetDatabase.AssetPathToGUID(paths[i]);
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = addresses[i];
                entry.SetLabel("after-hours-case-data", true, true, false);
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true, true);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("After Hours AssetBundle and Addressables lab inputs are ready.");
        }

        [MenuItem("Tritone/After Hours/Build Addressables Lab Content")]
        public static void BuildAddressablesContent()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null || settings.FindGroup("After Hours Local Content") == null)
            {
                Debug.LogError("Run Prepare Provider Labs before building Addressables content.");
                return;
            }
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (result == null || !string.IsNullOrEmpty(result.Error))
                throw new System.InvalidOperationException(result?.Error ?? "Addressables returned no build result.");
            Debug.Log("After Hours Addressables content built for the active profile and platform.");
        }

        [MenuItem("Tritone/After Hours/Build Local Bundle and Update Manifest")]
        public static void BuildLocalBundleAndManifest()
        {
            var importer = AssetImporter.GetAtPath(ClueTablePath);
            var prefabImporter = AssetImporter.GetAtPath(SystemsPanelPath);
            if (importer == null || importer.assetBundleName != BundleName ||
                prefabImporter == null || prefabImporter.assetBundleName != BundleName)
            {
                Debug.LogError("Run Prepare Provider Labs before building local content.");
                return;
            }

            string output = Path.Combine("ContentBuilds", "Current");
            var options = new ContentBuildOptions(
                PlayerSettings.bundleVersion,
                output,
                EditorUserBuildSettings.activeBuildTarget,
                assets: new[]
                {
                    new ContentBuildAsset("Tables/clues", ClueTablePath),
                    new ContentBuildAsset("UI/SystemsPanel", SystemsPanelPath)
                });
            var result = ContentBuildPipeline.Build(options);
            Debug.Log($"After Hours content built: {result.Manifest.Bundles.Count} bundle(s), " +
                      $"{result.Manifest.Assets.Count} address(es), manifest '{result.ManifestPath}'.");
        }
    }
}
#endif
