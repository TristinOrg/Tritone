#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using AfterHours.Bootstrap;
using AfterHours.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AfterHours.Editor
{
    /// <summary>Creates the small authored entry scenes without touching host scenes.</summary>
    public static class AfterHoursSceneSetup
    {
        private const string SceneFolder = "Assets/AfterHours/Scenes";
        private const string SampleFolder = "Assets/AfterHours";
        private const string HubGuid = "b945111aecf56954e96144effd227f62";

        [MenuItem("Tritone/After Hours/Open Sample Hub")]
        public static void GenerateSampleScenes()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (!EnsureSampleLocation())
                return;
            if (!AssetDatabase.IsValidFolder(SceneFolder))
                AssetDatabase.CreateFolder(SampleFolder, "Scenes");

            var hub = CreateEntryScene("SampleHub", typeof(AfterHoursHubBootstrap));
            CreateLabScene("TimerLab", typeof(AfterHoursCoreServicesLabBootstrap));
            CreateLabScene("EcsLab");
            CreateLabScene("UiLab", typeof(AfterHoursUiLabBootstrap));
            CreateLabScene("DataWorkshop", typeof(AfterHoursDataWorkshopBootstrap));
            CreateEntryScene("NetworkDesk", typeof(AfterHoursNetworkBootstrap));
            CreateEntryScene("AddressablesDesk", typeof(AfterHoursAddressablesBootstrap));
            CreateEntryScene("BundleDesk", typeof(AfterHoursAssetBundleBootstrap));
            CreateEntryScene("ContentWorkshop", typeof(AfterHoursContentBootstrap));
            foreach (var feature in AfterHoursFeatureCatalog.Load().Features)
            {
                if (string.IsNullOrEmpty(feature.ActionEn))
                    continue;
                CreateFocusedScene(feature);
            }
            RegisterSampleScenes();
            EditorSceneManager.OpenScene(hub, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log("Tritone Sample Hub is open. Press Play to choose a feature.");
        }

        [MenuItem("Tritone/After Hours/Generate Sample Scenes")]
        private static void OpenHubUsingLegacyMenu() => GenerateSampleScenes();

        private static bool EnsureSampleLocation()
        {
            if (AssetDatabase.IsValidFolder(SampleFolder))
                return true;

            string hubPath = AssetDatabase.GUIDToAssetPath(HubGuid);
            string folder  = Path.GetDirectoryName(Path.GetDirectoryName(hubPath))?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || !folder.StartsWith("Assets/", System.StringComparison.Ordinal))
            {
                Debug.LogError("Import the Tritone Feature Playground sample before opening its Hub.");
                return false;
            }

            string error = AssetDatabase.MoveAsset(folder, SampleFolder);
            if (string.IsNullOrEmpty(error))
                return true;

            Debug.LogError($"Could not prepare the After Hours sample: {error}");
            return false;
        }

        /// <summary>Registers the authored sample routes in the host Build Settings.</summary>
        public static void RegisterSampleScenes()
        {
            List<string> paths = new() { ScenePath("SampleHub") };
            foreach (var feature in AfterHoursFeatureCatalog.Load().Features)
                if (!string.IsNullOrEmpty(feature.ScenePath))
                    paths.Add(feature.ScenePath);
            EditorBuildSettings.scenes = MergeBuildScenes(EditorBuildSettings.scenes, paths);
        }

        private static void CreateFocusedScene(AfterHoursFeatureCatalog.Feature feature)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(feature.ScenePath) != null)
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCameraAndLight();
            var root = new GameObject("Tritone Feature Demo");
            if (feature.Id == "protocol-handshake-reconnect")
            {
                var network = new SerializedObject(root.AddComponent<AfterHoursNetworkBootstrap>());
                network.FindProperty("mObserveProtocol").boolValue = true;
                network.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                var lab = new SerializedObject(root.AddComponent<AfterHoursFocusedLabBootstrap>());
                lab.FindProperty("mFeatureId").stringValue = feature.Id;
                lab.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.SaveScene(scene, feature.ScenePath);
        }

        private static string CreateLabScene(string sceneName, System.Type runtimeBootstrapType = null)
        {
            string path = ScenePath(sceneName);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                return path;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCameraAndLight();
            if (runtimeBootstrapType == null)
            {
                var lab = new GameObject("After Hours Feature Lab");
                var component = lab.AddComponent<AfterHoursFeatureLabBootstrap>();
                var serialized = new SerializedObject(component);
                serialized.FindProperty("mLabId").stringValue = sceneName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            if (runtimeBootstrapType != null)
                new GameObject("After Hours Runtime").AddComponent(runtimeBootstrapType);
            if (sceneName == "EcsLab")
                new GameObject("After Hours ECS Runtime").AddComponent<AfterHoursEcsLabBootstrap>();
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string CreateEntryScene(string sceneName, System.Type bootstrapType)
        {
            string path = ScenePath(sceneName);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                return path;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCameraAndLight();
            var bootstrap = new GameObject("After Hours Bootstrap");
            bootstrap.AddComponent(bootstrapType);
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static void CreateCameraAndLight()
        {
            var cameraObject = new GameObject("After Hours Camera", typeof(Camera), typeof(AudioListener));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.098f, 0.11f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.tag = "MainCamera";
            var lightObject = new GameObject("After Hours Key Light", typeof(Light));
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.78f, 0.85f, 0.88f, 1f);
            light.intensity = 0.8f;
            lightObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
        }

        private static string ScenePath(string name) => Path.Combine(SceneFolder, name + ".unity").Replace('\\', '/');

        private static EditorBuildSettingsScene[] MergeBuildScenes(EditorBuildSettingsScene[] current, IEnumerable<string> additions)
        {
            var entries = new List<EditorBuildSettingsScene>(current);
            foreach (string path in additions)
            {
                int index = entries.FindIndex(entry => entry.path == path);
                if (index < 0)
                    entries.Add(new EditorBuildSettingsScene(path, true));
                else if (!entries[index].enabled)
                    entries[index] = new EditorBuildSettingsScene(path, true);
            }
            entries.RemoveAll(entry => entry.path == ScenePath("NeighborhoodCase"));
            entries.RemoveAll(entry => entry.path == ScenePath("AfterHoursEntry"));
            return entries.ToArray();
        }
    }
}
#endif
