using System;
using System.Collections.Generic;
using AfterHours.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AfterHours.Tests
{
    /// <summary>Verifies the sample catalog, scene registration, and scene bootstraps.</summary>
    public sealed class AfterHoursFeatureCatalogTests
    {
        private static readonly string[] RequiredFeatureIds =
        {
            "application-composition", "module-contexts-scopes", "diagnostics-logging",
            "main-thread-dispatch", "flows-transitions", "models-state", "entities-systems",
            "events", "timers", "tweens", "scene-modules-switching", "input", "audio",
            "ui-composition-generation", "pooling-ownership", "saves", "settings", "localization",
            "tables-generation-compilation", "network-tcp-messages", "protocol-handshake-reconnect",
            "resources-provider", "addressables-provider", "assetbundle-provider",
            "content-update-transactions", "editor-content-build", "editor-code-generation",
            "editor-provider-setup", "editor-addressables-build", "editor-ui-view-generation",
            "editor-ui-view-binding", "editor-network-code-generation", "editor-table-validation-and-generation",
            "editor-scene-authoring", "editor-active-platform-content-build"
        };

        [Test]
        public void BuildSettingsIncludeSampleHubAndFeatureScenes()
        {
            var previousScenes = EditorBuildSettings.scenes;
            try
            {
                AfterHoursSceneSetup.RegisterSampleScenes();
                Assert.That(Array.Exists(EditorBuildSettings.scenes, scene =>
                    scene.enabled && scene.path == "Assets/AfterHours/Scenes/SampleHub.unity"), Is.True);
                AllFeatureDestinationsAreIndependentAndValid();
            }
            finally
            {
                EditorBuildSettings.scenes = previousScenes;
            }
        }

        private static void AllFeatureDestinationsAreIndependentAndValid()
        {
            var asset = Resources.Load<TextAsset>("FeatureIndex/feature-index");
            Assert.That(asset, Is.Not.Null, "Feature Index JSON must be included with the sample.");
            var catalog = JsonUtility.FromJson<CatalogPayload>(asset.text);
            Assert.NotNull(catalog?.Features);
            Assert.Greater(catalog.Features.Length, 0, "Feature Index must contain demo and workflow routes.");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var scenePaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var feature in catalog.Features)
            {
                Assert.That(feature, Is.Not.Null);
                Assert.That(seen.Add(feature.Id), Is.True, "Duplicate feature id: " + feature.Id);
                Assert.That(string.IsNullOrWhiteSpace(feature.GroupId), Is.False, feature.Id + " has no group.");
                Assert.That(string.IsNullOrWhiteSpace(feature.TitleEn), Is.False, feature.Id + " has no English title.");
                Assert.That(string.IsNullOrWhiteSpace(feature.TitleZh), Is.False, feature.Id + " has no Chinese title.");
                Assert.That(string.IsNullOrWhiteSpace(feature.TitleJa), Is.False, feature.Id + " has no Japanese title.");
                Assert.That(string.IsNullOrWhiteSpace(feature.SummaryEn), Is.False, feature.Id + " has no English guidance.");
                Assert.That(string.IsNullOrWhiteSpace(feature.SummaryZh), Is.False, feature.Id + " has no Chinese guidance.");
                Assert.That(string.IsNullOrWhiteSpace(feature.SummaryJa), Is.False, feature.Id + " has no Japanese guidance.");
                Assert.That(string.IsNullOrWhiteSpace(feature.PrerequisiteEn), Is.False, feature.Id + " has no English prerequisite guidance.");
                Assert.That(string.IsNullOrWhiteSpace(feature.PrerequisiteZh), Is.False, feature.Id + " has no Chinese prerequisite guidance.");
                Assert.That(string.IsNullOrWhiteSpace(feature.PrerequisiteJa), Is.False, feature.Id + " has no Japanese prerequisite guidance.");

                bool hasScene = !string.IsNullOrWhiteSpace(feature.ScenePath);
                bool hasEditorRoute = !string.IsNullOrWhiteSpace(feature.EditorMenuPath);
                Assert.That(hasScene ^ hasEditorRoute, Is.True,
                            feature.Id + " must have exactly one scene or Editor menu destination.");
                if (hasEditorRoute)
                    Assert.That(feature.EditorMenuPath.StartsWith("Tritone/"), Is.True,
                                feature.Id + " must point to an exact Tritone menu command.");
                if (hasScene)
                {
                    Assert.That(scenePaths.Add(feature.ScenePath), Is.True,
                                $"Each runtime feature needs its own scene: {feature.Id} reuses {feature.ScenePath}.");
                    Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(feature.ScenePath), Is.Not.Null,
                                feature.Id + " points to a missing scene: " + feature.ScenePath);
                    Assert.That(Array.Exists(EditorBuildSettings.scenes, scene => scene.enabled && scene.path == feature.ScenePath),
                                Is.True, feature.Id + " points to a scene that is not in Build Settings.");
                }
            }

            foreach (string id in RequiredFeatureIds)
                Assert.That(seen.Contains(id), Is.True, "Missing documented Tritone capability: " + id);

            foreach (var feature in catalog.Features)
                if (!string.IsNullOrWhiteSpace(feature.ScenePath))
                    if (feature.ScenePath.IndexOf("AfterHoursEntry", StringComparison.Ordinal) >= 0)
                        throw new InvalidOperationException(feature.Id + " routes into the RPG instead of a focused feature demo.");

            var openedScenes = new List<Scene>();
            try
            {
                AssertSceneBootstrap("SampleHub", typeof(AfterHours.Bootstrap.AfterHoursHubBootstrap), openedScenes);
                AssertSceneBootstrap("EcsLab", typeof(AfterHours.Bootstrap.AfterHoursEcsLabBootstrap), openedScenes);
                AssertSceneBootstrap("TimerLab", typeof(AfterHours.Bootstrap.AfterHoursCoreServicesLabBootstrap), openedScenes);
                AssertSceneBootstrap("UiLab", typeof(AfterHours.Bootstrap.AfterHoursUiLabBootstrap), openedScenes);
                AssertSceneBootstrap("DataWorkshop", typeof(AfterHours.Bootstrap.AfterHoursDataWorkshopBootstrap), openedScenes);
                AssertSceneBootstrap("ContentWorkshop", typeof(AfterHours.Bootstrap.AfterHoursContentBootstrap), openedScenes);
                AssertSceneBootstrap("AddressablesDesk", typeof(AfterHours.Bootstrap.AfterHoursAddressablesBootstrap), openedScenes);
                AssertSceneBootstrap("BundleDesk", typeof(AfterHours.Bootstrap.AfterHoursAssetBundleBootstrap), openedScenes);
                AssertSceneBootstrap("NetworkDesk", typeof(AfterHours.Bootstrap.AfterHoursNetworkBootstrap), openedScenes);
                foreach (var feature in catalog.Features)
                {
                    if (string.IsNullOrWhiteSpace(feature.ActionEn))
                        continue;

                    Assert.That(feature.ActionZh, Is.Not.Empty, feature.Id);
                    Assert.That(feature.ActionJa, Is.Not.Empty, feature.Id);
                    var scene = OpenAdditive(feature.ScenePath, openedScenes);
                    if (feature.Id == "protocol-handshake-reconnect")
                    {
                        var network = FindOnRoot<AfterHours.Bootstrap.AfterHoursNetworkBootstrap>(scene);
                        Assert.That(network, Is.Not.Null, feature.Id);
                        Assert.That(network.ObservesProtocol, Is.True, feature.Id);
                    }
                    else
                    {
                        var lab = FindOnRoot<AfterHours.Bootstrap.AfterHoursFocusedLabBootstrap>(scene);
                        Assert.That(lab, Is.Not.Null, feature.Id);
                        Assert.That(lab.FeatureId, Is.EqualTo(feature.Id));
                    }
                }
            }
            finally
            {
                for (int i = openedScenes.Count - 1; i >= 0; i--)
                    if (openedScenes[i].IsValid() && openedScenes[i].isLoaded)
                        EditorSceneManager.CloseScene(openedScenes[i], true);
            }
        }

        private static void AssertSceneBootstrap(string sceneName, Type bootstrapType, List<Scene> openedScenes)
        {
            string path = $"Assets/AfterHours/Scenes/{sceneName}.unity";
            Scene scene = OpenAdditive(path, openedScenes);
            bool found = false;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent(bootstrapType) != null)
                    found = true;
            Assert.That(found, Is.True, sceneName + " must contain " + bootstrapType.Name + " to run a real Tritone feature demonstration.");
            if (sceneName == "SampleHub")
                return;
            if (!typeof(Tritone.Unity.TritoneBootstrap).IsAssignableFrom(bootstrapType))
                throw new InvalidOperationException(sceneName + " must start its own Tritone application, not the RPG bootstrap.");
            if (typeof(AfterHours.Bootstrap.AfterHoursBootstrap).IsAssignableFrom(bootstrapType))
                throw new InvalidOperationException(sceneName + " must not inherit the RPG application composition.");
        }

        private static Scene OpenAdditive(string path, List<Scene> openedScenes)
        {
            var scene = SceneManager.GetSceneByPath(path);
            if (scene.isLoaded)
                return scene;

            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            openedScenes.Add(scene);
            return scene;
        }

        private static T FindOnRoot<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent<T>(out var component))
                    return component;

            return null;
        }

        [Serializable]
        private sealed class CatalogPayload { public FeaturePayload[] Features; }
        [Serializable]
        private sealed class FeaturePayload
        {
            public string Id;
            public string GroupId;
            public string TitleEn;
            public string TitleZh;
            public string TitleJa;
            public string SummaryEn;
            public string SummaryZh;
            public string SummaryJa;
            public string PrerequisiteEn;
            public string PrerequisiteZh;
            public string PrerequisiteJa;
            public string ScenePath;
            public string EditorMenuPath;
            public string ActionEn;
            public string ActionZh;
            public string ActionJa;
        }
    }
}
