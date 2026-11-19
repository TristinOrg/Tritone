#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AfterHours.UI;
using Tritone.Unity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AfterHours.Editor
{
    /// <summary>Creates a typed sample window and calls Tritone's view generation/binding workflow.</summary>
    public static class AfterHoursUIAuthoring
    {
        private const string PrefabPath = "Assets/AfterHours/Runtime/Resources/UI/SystemsPanel.prefab";
        private const string TritoneEditorAssembly = "Tritone.Editor";
        private const string GeneratorType = "Tritone.Editor.UI.UIViewCodeGenerator";

        [MenuItem("Tritone/After Hours/Generate Systems Window View")]
        public static void GenerateSystemsWindowView()
        {
            if (!File.Exists(PrefabPath))
                CreatePrefab();
            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var prefabRef = prefab.GetComponent<UIPrefabRef>();
                var canvas = prefab.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                InvokeGenerator("Generate", prefabRef);
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Tritone/After Hours/Bind Generated Systems Window View")]
        public static void BindGeneratedSystemsWindowView()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException("Generate the sample UI prefab first.", PrefabPath);
            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var prefabRef = prefab.GetComponent<UIPrefabRef>();
                InvokeGenerator("Bind", prefabRef);
                if (prefab.GetComponent<AfterHoursSystemsPanelWindow>() == null)
                    prefab.AddComponent<AfterHoursSystemsPanelWindow>();
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
            AssetDatabase.Refresh();
        }

        private static void CreatePrefab()
        {
            string folder = Path.GetDirectoryName(PrefabPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }

            var root = new GameObject("SystemsPanel", typeof(RectTransform), typeof(CanvasRenderer),
                                      typeof(Image), typeof(Canvas), typeof(GraphicRaycaster), typeof(UIPrefabRef));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(620f, 330f);
            var background = root.GetComponent<Image>();
            background.color = new Color(0.075f, 0.098f, 0.11f, 0.98f);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 0;

            var bodyObject = CreateText(root.transform, "BodyText", string.Empty, 20,
                                        new Vector2(0f, 35f), new Vector2(520f, 180f), TextAnchor.UpperLeft);
            var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer),
                                             typeof(Image), typeof(Button));
            closeObject.transform.SetParent(root.transform, false);
            var closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.82f, 0.1f);
            closeRect.anchoredPosition = Vector2.zero;
            closeRect.sizeDelta = new Vector2(120f, 42f);
            closeObject.GetComponent<Image>().color = new Color(0.76f, 0.24f, 0.17f, 1f);
            CreateText(closeObject.transform, "Label", "CLOSE", 16, Vector2.zero,
                       new Vector2(112f, 38f), TextAnchor.MiddleCenter).GetComponent<Text>().color = Color.white;

            var prefabRef = root.GetComponent<UIPrefabRef>();
            prefabRef.ClassName = "SystemsPanelView";
            prefabRef.Namespace = "AfterHours.Generated.UI";
            prefabRef.OutputDirectory = "Assets/AfterHours/Runtime/Generated/UI";
            prefabRef.References = new[]
            {
                new UIViewReference { Name = "BodyText", Value = bodyObject.GetComponent<Text>(), Description = "Stores the explanatory copy displayed by the managed sample window." },
                new UIViewReference { Name = "CloseButton", Value = closeObject.GetComponent<Button>(), Description = "Closes the Tritone-managed popup window." }
            };
            prefabRef.SortingNodes = new[] { new UISortingNode { Target = canvas, RelativeOrder = 0, HierarchyDepth = 0 } };
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
        }

        private static GameObject CreateText(Transform parent, string name, string value, int fontSize,
                                             Vector2 position, Vector2 size, TextAnchor alignment)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(parent, false);
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.93f, 0.91f, 0.85f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static void InvokeGenerator(string methodName, UIPrefabRef prefabRef)
        {
            if (prefabRef == null)
                throw new InvalidOperationException("Systems panel prefab is missing UIPrefabRef authoring data.");
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => candidate.GetName().Name == TritoneEditorAssembly);
            Type generator = assembly?.GetType(GeneratorType);
            MethodInfo method = generator?.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(UIPrefabRef) }, null);
            if (method == null)
                throw new InvalidOperationException("Tritone's generated UIView workflow was not found in Tritone.Editor.");
            try
            {
                if (method.Invoke(null, new object[] { prefabRef }) is bool result && !result)
                    throw new InvalidOperationException($"Tritone's {methodName} step did not complete.");
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }
    }
}
#endif
