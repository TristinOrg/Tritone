using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace AfterHours.Editor
{
    /// <summary>Prepares the imported demos before they enter Play Mode.</summary>
    [InitializeOnLoad]
    internal static class AfterHoursPlayModePreparation
    {
        private const string SampleScenes = "Assets/AfterHours/Scenes/";
        private const string BuildStamp = "Library/Tritone/AfterHoursProviders.txt";
        private static readonly List<Process> Services = new();

        static AfterHoursPlayModePreparation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += StopServices;
            AssemblyReloadEvents.beforeAssemblyReload += StopServices;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                StopServices();
                return;
            }
            if (Application.isBatchMode)
                return;

            var startScene = EditorSceneManager.playModeStartScene;
            string path = startScene != null
                ? AssetDatabase.GetAssetPath(startScene)
                : SceneManager.GetActiveScene().path;
            if (!path.StartsWith(SampleScenes, StringComparison.Ordinal))
                return;

            try
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    AfterHoursSceneSetup.RegisterSampleScenes();
                    PrepareProviders();
                }
                else if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    StartService("relay.js", 27831, "AFTER_HOURS_RELAY_PORT");
                    StartService("content-host.js", 27832, "AFTER_HOURS_CONTENT_PORT");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Tritone sample setup could not finish: {exception.Message}");
            }
        }

        private static void PrepareProviders()
        {
            string fingerprint = string.Join("|", EditorUserBuildSettings.activeBuildTarget,
                PlayerSettings.bundleVersion,
                AssetDatabase.GetAssetDependencyHash(AfterHoursContentAuthoring.ClueTablePath),
                AssetDatabase.GetAssetDependencyHash(AfterHoursContentAuthoring.SystemsPanelPath));
            bool hasBundle = File.Exists("ContentBuilds/Current/after-hours-case-data") &&
                             File.Exists("ContentBuilds/Current/manifest.json");
            string addressablesRoot = "Library/com.unity.addressables/aa";
            bool hasAddressables = Directory.Exists(addressablesRoot) &&
                                   Directory.GetFiles(addressablesRoot, "settings.json", SearchOption.AllDirectories).Length > 0;
            if (hasBundle && hasAddressables && File.Exists(BuildStamp) && File.ReadAllText(BuildStamp) == fingerprint)
                return;

            AfterHoursContentAuthoring.PrepareProviderLabs();
            AfterHoursContentAuthoring.BuildAddressablesContent();
            AfterHoursContentAuthoring.BuildLocalBundleAndManifest();
            Directory.CreateDirectory(Path.GetDirectoryName(BuildStamp));
            File.WriteAllText(BuildStamp, string.Join("|", EditorUserBuildSettings.activeBuildTarget,
                PlayerSettings.bundleVersion,
                AssetDatabase.GetAssetDependencyHash(AfterHoursContentAuthoring.ClueTablePath),
                AssetDatabase.GetAssetDependencyHash(AfterHoursContentAuthoring.SystemsPanelPath)));
        }

        private static void StartService(string script, int port, string portVariable)
        {
            foreach (var endpoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
                if (endpoint.Port == port)
                    return;

            string scriptPath = Path.GetFullPath(Path.Combine("Assets/AfterHours/LocalServices", script));
            ProcessStartInfo start = new("node", $"\"{scriptPath}\"")
            {
                WorkingDirectory = Path.GetDirectoryName(Application.dataPath),
                CreateNoWindow = true,
                UseShellExecute = false
            };
            start.EnvironmentVariables[portVariable] = port.ToString();
            if (script == "content-host.js")
                start.EnvironmentVariables["AFTER_HOURS_CONTENT_ROOT"] = Path.GetFullPath("ContentBuilds/Current");
            try
            {
                var process = Process.Start(start);
                if (process != null)
                    Services.Add(process);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Debug.LogWarning($"The Tritone sample could not start {script}. Install Node.js and restart Unity to enable the local server demos.");
            }
        }

        private static void StopServices()
        {
            foreach (var process in Services)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch (InvalidOperationException) { }
                finally
                {
                    process.Dispose();
                }
            }
            Services.Clear();
        }
    }
}
