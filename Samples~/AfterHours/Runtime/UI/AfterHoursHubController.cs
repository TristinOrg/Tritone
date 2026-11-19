using System;
using System.Collections.Generic;
using System.IO;
using AfterHours.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AfterHours.UI
{
    /// <summary>Builds a compact, searchable-at-a-glance launcher from the feature catalog.</summary>
    public sealed class AfterHoursHubController : IDisposable
    {
        private static readonly Color Ink = new Color(0.035f, 0.055f, 0.063f, 1f);
        private static readonly Color Panel = new Color(0.075f, 0.105f, 0.115f, 1f);
        private static readonly Color Card = new Color(0.105f, 0.145f, 0.155f, 1f);
        private static readonly Color Paper = new Color(0.91f, 0.89f, 0.82f, 1f);
        private static readonly Color Muted = new Color(0.56f, 0.68f, 0.68f, 1f);
        private static readonly Color Signal = new Color(0.28f, 0.79f, 0.73f, 1f);

        private GameObject mRoot;
        private Text mStatus;
        private string mLocale;

        public void Show()
        {
            mLocale = AfterHoursFeatureCatalog.CurrentLocale();
            var catalog = AfterHoursFeatureCatalog.Load();
            var canvasObject = new GameObject("After Hours Sample Hub", typeof(RectTransform), typeof(Canvas),
                                                typeof(CanvasScaler), typeof(GraphicRaycaster));
            mRoot = canvasObject;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            scaler.matchWidthOrHeight = 0.5f;

            var background = Rect("Background", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            background.gameObject.AddComponent<Image>().color = Ink;

            var header = Rect("Header", background, new Vector2(0.055f, 0.79f), new Vector2(0.945f, 0.955f), Vector2.zero, Vector2.zero);
            header.gameObject.AddComponent<Image>().color = Panel;
            Text(header, "TRITONE   /   FEATURE PLAYGROUND", 20, Signal, TextAnchor.MiddleLeft);
            var title = Rect("Title", header, new Vector2(0.025f, 0.42f), new Vector2(0.7f, 0.92f), Vector2.zero, Vector2.zero);
            Text(title, Localized("Explore the whole toolkit", "完整探索 Tritone", "Tritone の機能を体験"), 34, Paper, TextAnchor.MiddleLeft, true);
            var subtitle = Rect("Subtitle", header, new Vector2(0.027f, 0.08f), new Vector2(0.87f, 0.4f), Vector2.zero, Vector2.zero);
            Text(subtitle, Localized("Small, independent demos. Choose one feature, try one action, see one result.",
                                     "独立的小型演示：选择一个功能，点一次操作，直接看结果。",
                                     "独立した小さなデモです。機能を選び、操作を 1 回試して結果を確認します。"), 16, Muted, TextAnchor.MiddleLeft);

            var footer = Rect("Footer", background, new Vector2(0.055f, 0.035f), new Vector2(0.945f, 0.09f), Vector2.zero, Vector2.zero);
            footer.gameObject.AddComponent<Image>().color = Panel;
            mStatus = Text(footer, Localized("Choose a card to launch a scene or open its Editor workflow.",
                                             "选择卡片进入场景，或查看对应的编辑器工作流。",
                                             "カードからシーンまたは Editor の作業手順を開きます。"), 14, Muted, TextAnchor.MiddleLeft);

            var scrollRect = new GameObject("Feature Index", typeof(RectTransform), typeof(Image), typeof(ScrollRect)).GetComponent<ScrollRect>();
            scrollRect.transform.SetParent(background, false);
            SetRect((RectTransform)scrollRect.transform, new Vector2(0.055f, 0.115f), new Vector2(0.945f, 0.765f), Vector2.zero, Vector2.zero);
            scrollRect.GetComponent<Image>().color = new Color(Ink.r, Ink.g, Ink.b, 0f);
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = Rect("Viewport", scrollRect.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.015f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = Rect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            var vertical = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 10f;
            vertical.padding = new RectOffset(2, 2, 2, 14);
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            Populate(content, catalog.Features);
        }

        private void Populate(Transform content, IReadOnlyList<AfterHoursFeatureCatalog.Feature> features)
        {
            var groupOrder = new List<string>();
            var groups = new Dictionary<string, List<AfterHoursFeatureCatalog.Feature>>(StringComparer.Ordinal);
            var destinations = new HashSet<string>(StringComparer.Ordinal);
            foreach (var feature in features)
            {
                if (!string.IsNullOrWhiteSpace(feature.EditorMenuPath) || string.IsNullOrWhiteSpace(feature.ScenePath))
                    continue;
                if (!destinations.Add(feature.ScenePath))
                    continue;
                if (!groups.TryGetValue(feature.GroupId, out var list))
                {
                    list = new List<AfterHoursFeatureCatalog.Feature>();
                    groups.Add(feature.GroupId, list);
                    groupOrder.Add(feature.GroupId);
                }
                list.Add(feature);
            }

            foreach (var groupId in groupOrder)
            {
                var group = groups[groupId];
                var heading = Rect("Group - " + groupId, content, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 34f));
                heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
                Text(heading, group[0].GroupTitle(mLocale).ToUpperInvariant(), 16, Signal, TextAnchor.MiddleLeft, true);
                foreach (var feature in group)
                    CreateCard(content, feature);
            }
        }

        private void CreateCard(Transform parent, AfterHoursFeatureCatalog.Feature feature)
        {
            var card = Rect("Feature - " + feature.Id, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 92f));
            card.gameObject.AddComponent<Image>().color = Card;
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = 92f;

            var action = card.gameObject.AddComponent<Button>();
            action.transition = Selectable.Transition.ColorTint;
            var colors = action.colors;
            colors.highlightedColor = new Color(0.15f, 0.23f, 0.235f, 1f);
            colors.pressedColor = Signal;
            action.colors = colors;
            action.onClick.AddListener(() => Open(feature));

            var titleRect = Rect("Title", card, new Vector2(0.025f, 0.57f), new Vector2(0.78f, 0.93f), Vector2.zero, Vector2.zero);
            Text(titleRect, feature.Title(mLocale), 18, Paper, TextAnchor.MiddleLeft, true);
            var summaryRect = Rect("Summary", card, new Vector2(0.027f, 0.25f), new Vector2(0.79f, 0.59f), Vector2.zero, Vector2.zero);
            Text(summaryRect, feature.Summary(mLocale), 13, Muted, TextAnchor.MiddleLeft);
            var prerequisiteRect = Rect("Prerequisite", card, new Vector2(0.027f, 0.04f), new Vector2(0.78f, 0.26f), Vector2.zero, Vector2.zero);
            Text(prerequisiteRect, feature.Prerequisite(mLocale), 11, Signal, TextAnchor.MiddleLeft);
            var routeRect = Rect("Route", card, new Vector2(0.80f, 0.1f), new Vector2(0.97f, 0.9f), Vector2.zero, Vector2.zero);
            Text(routeRect, string.IsNullOrWhiteSpace(feature.ScenePath) ? "EDITOR ↗" : "OPEN ↗", 12, Signal, TextAnchor.MiddleCenter, true);
        }

        private void Open(AfterHoursFeatureCatalog.Feature feature)
        {
            if (!string.IsNullOrWhiteSpace(feature.ScenePath))
            {
                string sceneName = Path.GetFileNameWithoutExtension(feature.ScenePath);
                if (!Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    SetStatus(Localized("Scene is not in Build Settings. Run Tritone / After Hours / Generate Sample Scenes.",
                                        "场景未加入 Build Settings。请运行 Tritone / After Hours / Generate Sample Scenes。",
                                        "シーンが Build Settings にありません。Tritone / After Hours / Generate Sample Scenes を実行してください。"));
                    return;
                }
                SceneManager.LoadScene(sceneName);
                return;
            }

            SetStatus(feature.Title(mLocale) + "  /  " + Localized("Open this Editor menu:", "请在 Unity 菜单中打开：", "次の Editor メニューを開いてください：") + " " + feature.EditorMenuPath);
        }

        private void SetStatus(string value)
        {
            if (mStatus != null) mStatus.text = value;
            Debug.Log("[After Hours] " + value);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            return rect;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Text Text(RectTransform parent, string value, int size, Color color, TextAnchor anchor, bool bold = false)
        {
            var rect = Rect("Text", parent, Vector2.zero, Vector2.one, new Vector2(10f, 2f), new Vector2(-10f, -2f));
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static string Localized(string english, string chinese, string japanese)
        {
            string locale = AfterHoursFeatureCatalog.CurrentLocale();
            return locale == "zh-CN" ? chinese : locale == "ja-JP" ? japanese : english;
        }

        public void Dispose()
        {
            if (mRoot != null) UnityEngine.Object.Destroy(mRoot);
            mRoot = null;
            mStatus = null;
        }
    }
}
