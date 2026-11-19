using AfterHours.Models;
using AfterHours.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

namespace AfterHours.UI
{
    /// <summary>Gives each feature scene a consistent, readable guide instead of an empty canvas.</summary>
    public sealed class AfterHoursFeatureLabController
    {
        private static readonly Color Ink = new Color(0.035f, 0.055f, 0.063f, 1f);
        private static readonly Color Panel = new Color(0.075f, 0.105f, 0.115f, 1f);
        private static readonly Color Card = new Color(0.105f, 0.145f, 0.155f, 1f);
        private static readonly Color Paper = new Color(0.91f, 0.89f, 0.82f, 1f);
        private static readonly Color Muted = new Color(0.56f, 0.68f, 0.68f, 1f);
        private static readonly Color Signal = new Color(0.28f, 0.79f, 0.73f, 1f);

        private readonly string mLabId;
        private string mLocale;
        private GameObject mRoot;
        private Text mEcsReadout;
        private Text mResult;
        private RectTransform mProgress;
        private Image[] mStages;
        private AfterHoursFeatureCatalog.Feature mFeature;
        private readonly Action mRunDemo;
        private readonly Action mResetDemo;

        public AfterHoursFeatureLabController(string labId, Action runDemo = null, Action resetDemo = null)
        {
            mLabId = labId ?? string.Empty;
            mRunDemo = runDemo;
            mResetDemo = resetDemo;
        }

        public void Show(int sortingOrder = 0)
        {
            mLocale = AfterHoursFeatureCatalog.CurrentLocale();
            mFeature = AfterHoursFeatureCatalog.Load().Find(mLabId);
            var canvas = new GameObject("After Hours Feature Lab", typeof(RectTransform), typeof(Canvas),
                                        typeof(CanvasScaler), typeof(GraphicRaycaster));
            mRoot = canvas;
            var guideCanvas = canvas.GetComponent<Canvas>();
            guideCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            guideCanvas.sortingOrder = sortingOrder;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            scaler.matchWidthOrHeight = 0.5f;

            var background = Rect("Background", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            background.gameObject.AddComponent<Image>().color = Ink;
            var header = Rect("Header", background, new Vector2(0.055f, 0.78f), new Vector2(0.945f, 0.955f), Vector2.zero, Vector2.zero);
            header.gameObject.AddComponent<Image>().color = Panel;
            Text(Rect("Eyebrow", header, new Vector2(0.025f, 0.65f), new Vector2(0.9f, 0.95f), Vector2.zero, Vector2.zero),
                 "TRITONE", 15, Signal, TextAnchor.MiddleLeft, true);
            Text(Rect("Title", header, new Vector2(0.025f, 0.22f), new Vector2(0.9f, 0.70f), Vector2.zero, Vector2.zero),
                 Title(), 32, Paper, TextAnchor.MiddleLeft, true);

            var body = Rect("Guide", background, new Vector2(0.055f, 0.2f), new Vector2(0.945f, 0.75f), Vector2.zero, Vector2.zero);
            body.gameObject.AddComponent<Image>().color = Card;
            Text(Rect("Purpose", body, new Vector2(0.035f, 0.69f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero),
                 mFeature?.Summary(mLocale) ?? Purpose(), 18, Paper, TextAnchor.MiddleLeft, true);
            var directions = Text(Rect("Try", body, new Vector2(0.035f, 0.39f), new Vector2(0.96f, 0.68f), Vector2.zero, Vector2.zero),
                                  mFeature?.Prerequisite(mLocale) ?? Directions(), 16, Muted, TextAnchor.UpperLeft);
            directions.resizeTextForBestFit = true;
            directions.resizeTextMinSize = 12;
            directions.resizeTextMaxSize = 16;
            Text(Rect("Result Label", body, new Vector2(0.035f, 0.30f), new Vector2(0.96f, 0.38f), Vector2.zero, Vector2.zero),
                 Localized("RESULT", "运行结果", "実行結果"), 13, Signal, TextAnchor.MiddleLeft);
            mResult = Text(Rect("Result", body, new Vector2(0.035f, 0.15f), new Vector2(0.96f, 0.30f), Vector2.zero, Vector2.zero),
                           ReadyText(), 15, Paper, TextAnchor.MiddleLeft, true);
            if (mLabId is "tweens" or "TimerLab")
            {
                var track = Rect("Tween Track", body, new Vector2(0.05f, 0.43f), new Vector2(0.94f, 0.48f), Vector2.zero, Vector2.zero);
                track.gameObject.AddComponent<Image>().color = Panel;
                mProgress = Rect("Tween Fill", track, Vector2.zero, Vector2.up, Vector2.zero, Vector2.zero);
                mProgress.gameObject.AddComponent<Image>().color = Signal;
            }
            if (mLabId == "pooling-ownership")
                AddStages(body, "RENT", "RETURN", "REUSE");
            else if (mLabId == "events")
                AddStages(body, "PUBLISHER", "TYPED EVENT", "LISTENER");
            else if (mLabId == "input")
                AddStages(body, "BUTTON / KEY", "INTERACT", "SUBSCRIBER");
            else if (mLabId == "flows-transitions")
                AddStages(body, "READY", "RUNNING");
            else if (mLabId == "scene-modules-switching")
            {
                AddStages(body, "LOBBY", "ROUND");
                SetStage(0);
            }
            if (mLabId == "EcsLab")
            {
                mResult.gameObject.SetActive(false);
                AddEcsControls(body);
            }
            else
                AddDemoControls(body);

            var footer = Rect("Footer", background, new Vector2(0.055f, 0.07f), new Vector2(0.945f, 0.16f), Vector2.zero, Vector2.zero);
            footer.gameObject.AddComponent<Image>().color = Panel;
            var buttonRect = Rect("Back To Hub", footer, new Vector2(0.70f, 0.12f), new Vector2(0.985f, 0.88f), Vector2.zero, Vector2.zero);
            var button = buttonRect.gameObject.AddComponent<Image>();
            button.color = Signal;
            var back = buttonRect.gameObject.AddComponent<Button>();
            back.onClick.AddListener(ReturnToHub);
            Text(buttonRect, Localized("BACK TO SAMPLE HUB", "返回示例目录", "サンプル一覧へ戻る"), 14, Ink, TextAnchor.MiddleCenter, true);
            Text(Rect("Help", footer, new Vector2(0.02f, 0.12f), new Vector2(0.68f, 0.88f), Vector2.zero, Vector2.zero),
                 Localized("Use the Hub at any time to switch labs. Optional profiles list their setup requirements there.",
                           "可随时返回目录切换实验室；可选配置的前置条件已标在目录中。",
                           "Hub からいつでも別のラボへ移動できます。任意プロファイルの要件は一覧に記載されています。"),
                 13, Muted, TextAnchor.MiddleLeft);
        }

        public void Tick()
        {
            if (mEcsReadout == null) return;
            mEcsReadout.text = Localized(
                $"SCENE WORLD / {AfterHours.Modules.AfterHoursEcsLabWorld.Count:00} ENTITIES / SYSTEM PULSE {AfterHours.Modules.AfterHoursEcsLabWorld.FirstPulse:0.00}",
                $"场景 World / {AfterHours.Modules.AfterHoursEcsLabWorld.Count:00} 个实体 / 系统脉冲 {AfterHours.Modules.AfterHoursEcsLabWorld.FirstPulse:0.00}",
                $"Scene World / Entity {AfterHours.Modules.AfterHoursEcsLabWorld.Count:00} / System Pulse {AfterHours.Modules.AfterHoursEcsLabWorld.FirstPulse:0.00}");
        }

        public void SetResult(string value)
        {
            if (mResult != null)
                mResult.text = value ?? string.Empty;
        }

        public void SetProgress(float progress)
        {
            if (mProgress != null)
                mProgress.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        }

        public void ResetResult()
        {
            SetResult(ReadyText());
            SetProgress(0f);
            SetStage(mLabId == "scene-modules-switching" ? 0 : -1);
        }

        private void AddStages(RectTransform body, params string[] labels)
        {
            mStages = new Image[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                float left = 0.05f + 0.30f * i;
                var cell = Rect(labels[i], body, new Vector2(left, 0.41f),
                                new Vector2(left + 0.28f, 0.53f), Vector2.zero, Vector2.zero);
                mStages[i] = cell.gameObject.AddComponent<Image>();
                Text(cell, labels[i], 13, Paper, TextAnchor.MiddleCenter, true);
            }
            SetStage(-1);
        }

        public void SetStage(int index)
        {
            if (mStages == null)
                return;
            for (int i = 0; i < mStages.Length; i++)
                mStages[i].color = i == index ? new Color(0.15f, 0.48f, 0.42f) : Panel;
        }

        private void AddDemoControls(RectTransform body)
        {
            var runRect = Rect("Run Demo", body, new Vector2(0.035f, 0.025f), new Vector2(0.48f, 0.14f), Vector2.zero, Vector2.zero);
            runRect.gameObject.AddComponent<Image>().color = Signal;
            runRect.gameObject.AddComponent<Button>().onClick.AddListener(() => mRunDemo?.Invoke());
            Text(runRect, mFeature?.ActionLabel(mLocale) ?? DemoLabel(), 13, Ink, TextAnchor.MiddleCenter, true);

            var resetRect = Rect("Reset Demo", body, new Vector2(0.52f, 0.025f), new Vector2(0.96f, 0.14f), Vector2.zero, Vector2.zero);
            resetRect.gameObject.AddComponent<Image>().color = Panel;
            resetRect.gameObject.AddComponent<Button>().onClick.AddListener(() => mResetDemo?.Invoke());
            Text(resetRect, Localized("RESET", "重置", "リセット"), 13, Paper, TextAnchor.MiddleCenter, true);
        }

        private void AddEcsControls(RectTransform body)
        {
            var readoutRect = Rect("Entity Readout", body, new Vector2(0.035f, 0.15f), new Vector2(0.96f, 0.30f), Vector2.zero, Vector2.zero);
            mEcsReadout = Text(readoutRect, Localized("SCENE WORLD / STARTING...", "场景 World / 正在启动…", "Scene World / 起動中…"),
                               12, Signal, TextAnchor.MiddleLeft, true);
            ActionButton(body, "Spawn Entity", Localized("+ SPAWN SIGNAL", "+ 生成信号实体", "+ Signal Entity"),
                         AfterHours.Modules.AfterHoursEcsLabWorld.Spawn,
                         new Vector2(0.47f, 0.025f), new Vector2(0.69f, 0.14f));
            ActionButton(body, "Reset World", Localized("RESET WORLD", "重置 World", "World をリセット"),
                         AfterHours.Modules.AfterHoursEcsLabWorld.Reset,
                         new Vector2(0.72f, 0.025f), new Vector2(0.96f, 0.14f));
        }

        private static void ActionButton(Transform parent, string name, string caption, UnityEngine.Events.UnityAction callback,
                                         Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = Rect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            rect.gameObject.AddComponent<Image>().color = Signal;
            rect.gameObject.AddComponent<Button>().onClick.AddListener(callback);
            Text(rect, caption, 12, Ink, TextAnchor.MiddleCenter, true);
        }

        private string Title() => mFeature?.Title(mLocale) ?? Localized(
            mLabId == "TimerLab" ? "Timer Lab" : mLabId == "EcsLab" ? "ECS Lab" :
            mLabId == "UiLab" ? "UI Lab" : mLabId == "DataWorkshop" ? "Typed Tables" :
            mLabId == "ContentWorkshop" ? "Content Update" : mLabId == "AddressablesDesk" ? "Addressables" :
            mLabId == "BundleDesk" ? "AssetBundle" : mLabId == "NetworkDesk" ? "Network" : mLabId,
            mLabId == "TimerLab" ? "计时器演示" : mLabId == "EcsLab" ? "ECS 实验室" :
            mLabId == "UiLab" ? "UI 实验室" : mLabId == "DataWorkshop" ? "类型化数据表" :
            mLabId == "ContentWorkshop" ? "内容更新" : mLabId == "AddressablesDesk" ? "Addressables" :
            mLabId == "BundleDesk" ? "AssetBundle" : mLabId == "NetworkDesk" ? "网络" : mLabId,
            mLabId == "TimerLab" ? "Timer デモ" : mLabId == "EcsLab" ? "ECS ラボ" :
            mLabId == "UiLab" ? "UI ラボ" : mLabId == "DataWorkshop" ? "型付き Table" :
            mLabId == "ContentWorkshop" ? "Content Update" : mLabId == "AddressablesDesk" ? "Addressables" :
            mLabId == "BundleDesk" ? "AssetBundle" : mLabId == "NetworkDesk" ? "Network" : mLabId);

        private string Purpose()
        {
            switch (mLabId)
            {
                case "EcsLab": return Localized("This scene owns a real Tritone Scene World. Spawn markers, watch the query count and system pulse update, then reset or leave the scene to release the world.",
                    "本场景运行真实的 Tritone Scene World。生成标记后观察查询数量和系统脉冲变化；也可重置，或离开场景释放 World。",
                    "このシーンは Tritone の Scene World を所有します。Marker を生成して Query 件数と System Pulse を確認し、リセットまたはシーン終了で World を解放します。");
                case "UiLab": return Localized("This scene starts Tritone UI on its own. Open the sample window, inspect its popup layer and generated view, then close or reopen it here.",
                    "本场景独立启动 Tritone UI。打开示例窗口，查看 Popup 层和生成 View，再在本场景关闭或重开。",
                    "このシーンは Tritone UI を単独で起動します。サンプル Window を開き、Popup Layer と生成 View を確認して閉じるか再表示します。");
                case "DataWorkshop": return Localized("Load one typed Clue table through Tritone. The result shows the row count and first generated row.",
                    "通过 Tritone 加载一张类型化 Clue 表。结果会显示行数和第一条生成数据。",
                    "Tritone で型付き Clue Table を読み込みます。行数と最初の生成 Row が表示されます。");
                case "AddressablesDesk": return Localized("Load the sample clue asset through the Addressables profile. Build Addressables content first.", "先构建 Addressables 内容，再通过 Addressables 配置加载示例线索资源。", "Addressables Content を構築してから、Addressables Profile 経由で Clue Asset を読み込みます。");
                case "BundleDesk": return Localized("Load the sample clue asset through the AssetBundle profile. Build the local bundle first.", "先构建本地 Bundle，再通过 AssetBundle 配置加载示例线索资源。", "ローカル Bundle を構築してから、AssetBundle Profile 経由で Clue Asset を読み込みます。");
                case "ContentWorkshop": return Localized("Check and apply one local content update. If the host is unavailable, the result explains what to start.", "检查并应用一次本地内容更新；如果服务未启动，结果会提示下一步。", "ローカル Content Update を 1 回確認・適用します。Host が未起動の場合は必要な操作を表示します。");
                case "NetworkDesk": return Localized("Send one generated CaseStatus message to the optional loopback relay.", "向可选的本机回环 Relay 发送一条生成的 CaseStatus 消息。", "生成済み CaseStatus Message を任意の Loopback Relay に 1 件送信します。");
                default: return Localized("Run one Tritone timer and watch its callback arrive. Reset cancels the pending timer.",
                    "运行一个 Tritone 计时器并观察回调；重置会取消等待中的计时器。",
                    "Tritone Timer を 1 つ実行し Callback を確認します。リセットすると待機中の Timer をキャンセルします。");
            }
        }

        private string Directions()
        {
            switch (mLabId)
            {
                case "EcsLab": return Localized("1. Two sample EvidenceMarker entities are seeded on entry.\n2. Click + SPAWN SIGNAL; the live entity count increases and SignalPresentationSystem advances the pulse.\n3. Click RESET WORLD to destroy every lab entity.\n4. Return to the Hub and re-enter to see a fresh scene-owned world.\n\nComponent: EvidenceMarkerComponent\nSystem: SignalPresentationSystem",
                    "1. 进入场景时会创建两个 EvidenceMarker 实体。\n2. 点击「+ 生成信号实体」；实体数量增加，SignalPresentationSystem 推进脉冲值。\n3. 点击「重置 World」销毁本实验室的所有实体。\n4. 返回目录并重新进入，观察重新创建的场景 World。\n\n组件：EvidenceMarkerComponent\n系统：SignalPresentationSystem",
                    "1. 入場時に EvidenceMarker Entity が 2 つ生成されます。\n2. 「+ Signal Entity」を押すと Entity 数と SignalPresentationSystem の Pulse が変化します。\n3. 「World をリセット」でラボ内の Entity をすべて破棄します。\n4. Hub に戻って再入場し、新しい Scene World を確認します。\n\nComponent: EvidenceMarkerComponent\nSystem: SignalPresentationSystem");
                case "UiLab": return Localized("1. Click OPEN WINDOW.\n2. Read the sample window and notice it is placed on Tritone's Popup layer.\n3. Close it with its own button, then click OPEN WINDOW again.\n4. Return to the Hub when finished.",
                    "1. 点击 OPEN WINDOW。\n2. 查看示例窗口，并留意它位于 Tritone 的 Popup 层。\n3. 用窗口自己的按钮关闭，再次点击 OPEN WINDOW。\n4. 完成后返回目录。",
                    "1. OPEN WINDOW をクリックします。\n2. サンプル Window を確認し、Tritone の Popup Layer に配置されていることを見ます。\n3. Window 内のボタンで閉じ、もう一度 OPEN WINDOW を押します。\n4. 終了したら Hub に戻ります。");
                case "DataWorkshop": return Localized("1. Click LOAD TABLE.\n2. Tritone reads Resources/Tables/clues.json into the generated ClueRow type.\n3. Read the row count and first row in the result.\n4. Click LOAD TABLE again to repeat, or Reset to clear the result.",
                    "1. 点击 LOAD TABLE。\n2. Tritone 将 Resources/Tables/clues.json 读取为生成的 ClueRow 类型。\n3. 查看结果里的行数和第一条数据。\n4. 可再次加载，或重置清除结果。",
                    "1. LOAD TABLE をクリックします。\n2. Tritone が Resources/Tables/clues.json を生成済み ClueRow 型に読み込みます。\n3. 結果に表示される行数と最初の Row を確認します。\n4. 再読込またはリセットで結果を消去します。");
                case "AddressablesDesk": return Localized("1. Build content with Tritone → After Hours → Build Addressables Lab Content.\n2. Click LOAD ASSET.\n3. Confirm the clue JSON loaded through the Addressables provider.\n4. Reset to release the asset scope.", "1. 运行 Tritone → After Hours → Build Addressables Lab Content。\n2. 点击 LOAD ASSET。\n3. 确认线索 JSON 由 Addressables Provider 加载。\n4. 重置以释放资源作用域。", "1. Tritone → After Hours → Build Addressables Lab Content を実行します。\n2. LOAD ASSET を押します。\n3. Addressables Provider 経由で Clue JSON が読み込まれたことを確認します。\n4. Reset で Asset Scope を解放します。");
                case "BundleDesk": return Localized("1. Run Prepare Provider Labs, then Build Local Bundle and Update Manifest.\n2. Click LOAD ASSET.\n3. Confirm the clue JSON loaded through the AssetBundle provider.\n4. Reset to release the asset scope.", "1. 运行 Prepare Provider Labs，再运行 Build Local Bundle and Update Manifest。\n2. 点击 LOAD ASSET。\n3. 确认线索 JSON 由 AssetBundle Provider 加载。\n4. 重置以释放资源作用域。", "1. Prepare Provider Labs、続けて Build Local Bundle and Update Manifest を実行します。\n2. LOAD ASSET を押します。\n3. AssetBundle Provider 経由で Clue JSON が読み込まれたことを確認します。\n4. Reset で Asset Scope を解放します。");
                case "ContentWorkshop": return Localized("1. Run Prepare Provider Labs and Build Local Bundle and Update Manifest.\n2. Start LocalServices/content-host.js with Node.js.\n3. Click CHECK UPDATE and watch the status.\n4. Stop the host or return to Hub when done.", "1. 运行 Prepare Provider Labs 和 Build Local Bundle and Update Manifest。\n2. 使用 Node.js 启动 LocalServices/content-host.js。\n3. 点击 CHECK UPDATE 并观察状态。\n4. 完成后停止服务或返回目录。", "1. Prepare Provider Labs と Build Local Bundle and Update Manifest を実行します。\n2. Node.js で LocalServices/content-host.js を起動します。\n3. CHECK UPDATE を押して状態を確認します。\n4. 終了後 Host を停止するか Hub に戻ります。");
                case "NetworkDesk": return Localized("1. Run node LocalServices/relay.js.\n2. Click SEND REQUEST.\n3. Tritone connects to 127.0.0.1:27831 and sends a generated CaseStatus message.\n4. Reset disconnects; no remote server is used.", "1. 运行 node LocalServices/relay.js。\n2. 点击 SEND REQUEST。\n3. Tritone 连接 127.0.0.1:27831 并发送生成的 CaseStatus 消息。\n4. 重置会断开连接；不会访问远程服务器。", "1. node LocalServices/relay.js を起動します。\n2. SEND REQUEST を押します。\n3. Tritone が 127.0.0.1:27831 に接続し、生成済み CaseStatus Message を送信します。\n4. Reset で切断します。外部サーバーは使用しません。");
                default: return Localized("1. Click RUN TIMER.\n2. The status changes to waiting, then reports when Tritone fires the one-shot timer.\n3. Click RESET to cancel a pending timer and return to Ready.\n4. Use Back to Hub to choose another feature.",
                    "1. 点击 RUN TIMER。\n2. 状态先显示等待，Tritone 触发一次性计时器后会显示结果。\n3. 点击 RESET 取消等待中的计时器并回到 Ready。\n4. 用 Back to Hub 选择其他功能。",
                    "1. RUN TIMER をクリックします。\n2. 待機表示の後、Tritone の One-shot Timer が実行された結果が表示されます。\n3. RESET で待機中の Timer をキャンセルして Ready に戻します。\n4. Back to Hub から別の機能を選びます。");
            }
        }

        private string DemoLabel()
        {
            switch (mLabId)
            {
                case "EcsLab": return Localized("+ SPAWN SIGNAL", "+ 生成信号实体", "+ Signal Entity");
                case "UiLab": return Localized("OPEN WINDOW", "打开窗口", "Window を開く");
                case "DataWorkshop": return Localized("LOAD TABLE", "加载数据表", "Table を読込");
                case "AddressablesDesk":
                case "BundleDesk": return Localized("LOAD ASSET", "加载资源", "Asset を読込");
                case "ContentWorkshop": return Localized("CHECK UPDATE", "检查更新", "更新を確認");
                case "NetworkDesk": return Localized("SEND REQUEST", "发送请求", "Request を送信");
                default: return Localized("RUN TIMER", "运行计时器", "Timer を実行");
            }
        }

        private string ReadyText() => mLabId == "EcsLab"
            ? Localized("SCENE WORLD / READY", "场景 World / 就绪", "Scene World / Ready")
            : mLabId == "ContentWorkshop"
                ? Localized("READY / start the optional local content host.", "就绪 / 请先启动可选的本地内容服务。", "準備完了 / 任意のローカル Content Host を起動してください。")
                : mLabId == "AddressablesDesk" || mLabId == "BundleDesk"
                    ? Localized("READY / build this profile's content first.", "就绪 / 请先构建此配置的内容。", "準備完了 / 先に Profile の Content を構築してください。")
                    : mLabId == "NetworkDesk"
                        ? Localized("READY / start the optional local relay.", "就绪 / 请先启动可选的本地 Relay。", "準備完了 / 任意のローカル Relay を起動してください。")
                        : Localized("READY / choose the highlighted action.", "就绪 / 点击高亮按钮。", "準備完了 / 強調表示のボタンを押してください。");

        private void ReturnToHub()
        {
            if (!Application.CanStreamedLevelBeLoaded("SampleHub"))
            {
                SetResult(Localized("Register the sample scenes from the Tritone / After Hours menu first.",
                                    "请先从 Tritone / After Hours 菜单注册示例场景。",
                                    "先に Tritone / After Hours メニューでサンプルシーンを登録してください。"));
                return;
            }

            var bootstrap = UnityEngine.Object.FindObjectOfType<Tritone.Unity.TritoneBootstrap>();
            if (bootstrap != null)
                UnityEngine.Object.Destroy(bootstrap.gameObject);
            SceneManager.LoadScene("SampleHub");
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static Text Text(RectTransform parent, string value, int size, Color color, TextAnchor anchor, bool bold = false)
        {
            var rect = Rect("Text", parent, Vector2.zero, Vector2.one, new Vector2(14f, 8f), new Vector2(-14f, -8f));
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
            mEcsReadout = null;
        }
    }
}
