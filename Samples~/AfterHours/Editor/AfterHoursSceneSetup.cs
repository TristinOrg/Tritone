#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using AfterHours.Bootstrap;
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

        [MenuItem("Tritone/After Hours/Generate Sample Scenes")]
        public static void GenerateSampleScenes()
        {
            if (!AssetDatabase.IsValidFolder("Assets/AfterHours"))
            {
                Debug.LogError("Import the Tritone Feature Playground sample before generating its scenes.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(SceneFolder))
                AssetDatabase.CreateFolder("Assets/AfterHours", "Scenes");

            var hub = CreateEntryScene("SampleHub", typeof(AfterHoursHubBootstrap));
            CreateLabScene("CoreServicesLab", typeof(AfterHoursCoreServicesLabBootstrap));
            CreateLabScene("EcsLab");
            CreateLabScene("UiLab", typeof(AfterHoursUiLabBootstrap));
            CreateLabScene("DataWorkshop", typeof(AfterHoursDataWorkshopBootstrap));
            CreateEntryScene("NetworkDesk", typeof(AfterHoursNetworkBootstrap));
            CreateEntryScene("AddressablesDesk", typeof(AfterHoursAddressablesBootstrap));
            CreateEntryScene("BundleDesk", typeof(AfterHoursAssetBundleBootstrap));
            CreateEntryScene("ContentWorkshop", typeof(AfterHoursContentBootstrap));
            RegisterSampleScenes();
            EditorSceneManager.OpenScene(hub, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log("After Hours scenes generated and added to build settings. The host integration scene was not changed.");
        }

        /// <summary>Registers the authored sample routes in the host Build Settings.</summary>
        public static void RegisterSampleScenes()
        {
            EditorBuildSettings.scenes = MergeBuildScenes(EditorBuildSettings.scenes,
                ScenePath("SampleHub"), ScenePath("CoreServicesLab"), ScenePath("EcsLab"),
                ScenePath("UiLab"), ScenePath("DataWorkshop"),
                ScenePath("NetworkDesk"),
                ScenePath("AddressablesDesk"), ScenePath("BundleDesk"),
                ScenePath("ContentWorkshop"));
        }

        private static string CreateLabScene(string sceneName, System.Type runtimeBootstrapType = null)
        {
            string path = ScenePath(sceneName);
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

        private static EditorBuildSettingsScene[] MergeBuildScenes(EditorBuildSettingsScene[] current, params string[] additions)
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
