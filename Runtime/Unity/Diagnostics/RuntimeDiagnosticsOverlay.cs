using Tritone.Diagnostics;
using UnityEngine;

namespace Tritone.Unity.Diagnostics
{
    /// <summary>
    /// Draws an optional immediate-mode runtime diagnostics window.
    /// </summary>
    public sealed class RuntimeDiagnosticsOverlay : MonoBehaviour
    {
        /// <summary>
        /// Stores the key that toggles the diagnostics window.
        /// </summary>
        public KeyCode ToggleKey = KeyCode.F3;

        /// <summary>
        /// Stores the movable diagnostics window rectangle.
        /// </summary>
        public Rect WindowRect = new(10.0f, 10.0f, 520.0f, 500.0f);

        /// <summary>
        /// Stores the maximum number of recent logs displayed.
        /// </summary>
        public int MaxVisibleLogs = 12;

        /// <summary>
        /// Stores whether the window is initially visible.
        /// </summary>
        public bool Visible = true;

        /// <summary>
        /// Stores the cached window callback delegate.
        /// </summary>
        private GUI.WindowFunction mDrawWindow;

        /// <summary>
        /// Caches the window callback once for the component lifetime.
        /// </summary>
        private void Awake()
        {
            mDrawWindow = DrawWindow;
        }

        /// <summary>
        /// Toggles the runtime panel from the configured key.
        /// </summary>
        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(ToggleKey))
                Visible = !Visible;
        }

        /// <summary>
        /// Draws the runtime diagnostics window when enabled.
        /// </summary>
        private void OnGUI()
        {
            if (!Visible)
                return;

            var application = TritoneBootstrap.Current;
            if (application == null || !application.Services.TryGet<IRuntimeDiagnosticsService>(out _))
                return;

            WindowRect = GUI.Window(GetInstanceID(), WindowRect, mDrawWindow, "Tritone Diagnostics");
        }

        /// <summary>
        /// Draws snapshot values and recent buffered logs.
        /// </summary>
        /// <param name="windowId">The Unity immediate-mode window identifier.</param>
        private void DrawWindow(int windowId)
        {
            var diagnostics = TritoneBootstrap.Current.Services.GetRequired<IRuntimeDiagnosticsService>();
            var snapshot    = diagnostics.Snapshot;
            GUILayout.Label($"Application: {snapshot.ApplicationState}");
            GUILayout.Label($"Module: {snapshot.ActiveModule ?? "-"}");
            GUILayout.Label($"Flow: {snapshot.ActiveFlow ?? "-"}");
            GUILayout.Label($"FPS: {snapshot.FramesPerSecond:F1}  Frame: {snapshot.AverageFrameMilliseconds:F2} ms");
            GUILayout.Label($"Frame min/max: {snapshot.MinimumFrameMilliseconds:F2} / {snapshot.MaximumFrameMilliseconds:F2} ms");
            GUILayout.Label($"Entities: app {snapshot.ApplicationEntities}, scene {snapshot.SceneEntities}");
            GUILayout.Space(8.0f);
            GUILayout.Label("Recent logs");

            var logs  = diagnostics.Logs;
            var start = Mathf.Max(0, logs.Count - Mathf.Max(0, MaxVisibleLogs));
            for (var i = start; i < logs.Count; i++)
            {
                var logEvent = logs.GetAt(i);
                GUILayout.Label($"[{logEvent.Level}] {logEvent.Category}: {logEvent.Message}");
            }

            GUI.DragWindow(new Rect(0.0f, 0.0f, WindowRect.width, 24.0f));
        }

        /// <summary>
        /// Releases the cached callback reference.
        /// </summary>
        private void OnDestroy()
        {
            mDrawWindow = null;
        }
    }
}
