using AfterHours.UI;
using Tritone.Kernel;
using Tritone.UI;
using Tritone.Unity;
using Tritone.Unity.Assets;
using Tritone.Unity.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AfterHours.Bootstrap
{
    /// <summary>Demonstrates one Tritone-managed popup without starting the RPG.</summary>
    public sealed class AfterHoursUiLabBootstrap : TritoneBootstrap
    {
        private UIRoot mRoot;
        private AfterHoursFeatureLabController mController;
        private IUIWindowScope mWindows;
        private IUIService mUI;
        private AfterHoursSystemsPanelWindow mPopup;

        protected override void Configure(GameApplicationBuilder builder) => builder.UseAssets().UseUI(mRoot);

        protected override void Awake()
        {
            EnsureEventSystem();
            CreateRoot();
            base.Awake();
            mUI = Application.Services.GetRequired<IUIService>();
            mWindows = mUI.CreateScope();
            mWindows.AddWindow(typeof(AfterHoursSystemsPanelWindow), "UI/SystemsPanel", EUILayer.Popup, EUIWindowLifetime.Application);
            mController = new AfterHoursFeatureLabController("UiLab", ToggleWindow, ResetWindow);
            // Tritone starts its Popup sorting range at -5000.
            mController.Show(sortingOrder: -10000);
        }

        private void ToggleWindow()
        {
            if (mUI.IsWindowOpen(typeof(AfterHoursSystemsPanelWindow)))
            {
                mUI.CloseWindow(typeof(AfterHoursSystemsPanelWindow));
                mController.SetResult("WINDOW CLOSED / Click Open Window to show it again.");
                return;
            }

            var popup = (AfterHoursSystemsPanelWindow)mUI.OpenWindow(typeof(AfterHoursSystemsPanelWindow));
            if (mPopup != popup)
            {
                mPopup = popup;
                mPopup.Closed += OnPopupClosed;
            }
            mController.SetResult("The popup is on screen. Use its CLOSE button to dismiss it.");
        }

        private void OnPopupClosed() =>
            mController?.SetResult("WINDOW CLOSED / Click Open Window to show it again.");

        private void ResetWindow()
        {
            if (mUI != null && mUI.IsWindowOpen(typeof(AfterHoursSystemsPanelWindow)))
                mUI.CloseWindow(typeof(AfterHoursSystemsPanelWindow));
            mController?.SetResult("READY / Click Open Window to show the popup.");
        }

        protected override void OnDestroy()
        {
            if (mPopup != null)
                mPopup.Closed -= OnPopupClosed;
            mController?.Dispose();
            mWindows?.Dispose();
            mController = null;
            mWindows = null;
            mUI = null;
            base.OnDestroy();
        }

        private void CreateRoot()
        {
            var canvasObject = new GameObject("Tritone UI Lab Root", typeof(RectTransform), typeof(Canvas),
                                               typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UIRoot));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            scaler.matchWidthOrHeight = 0.5f;
            mRoot = canvasObject.GetComponent<UIRoot>();
            mRoot.Background = CreateLayer(canvasObject.transform, "Background");
            mRoot.Normal = CreateLayer(canvasObject.transform, "Normal");
            mRoot.Popup = CreateLayer(canvasObject.transform, "Popup");
            mRoot.Guide = CreateLayer(canvasObject.transform, "Guide");
            mRoot.Loading = CreateLayer(canvasObject.transform, "Loading");
            mRoot.System = CreateLayer(canvasObject.transform, "System");
        }

        private static RectTransform CreateLayer(Transform parent, string name)
        {
            var layer = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            layer.SetParent(parent, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            return layer;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("After Hours Event System", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
