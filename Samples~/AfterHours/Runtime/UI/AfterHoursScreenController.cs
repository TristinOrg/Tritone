using System;
using System.Collections.Generic;
using AfterHours.Domain;
using AfterHours.Bootstrap;
using AfterHours.Models;
using AfterHours.Modules;
using Tritone.Unity.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AfterHours.UI
{
    /// <summary>Builds the sample's responsive, vector-only field interface.</summary>
    public sealed class AfterHoursScreenController : IDisposable
    {
        private static readonly Color Ink = Hex("182126");
        private static readonly Color Paper = Hex("E9E4D9");
        private static readonly Color Muted = Hex("9FA8A5");
        private static readonly Color Cyan = Hex("74D4D0");
        private static readonly Color Vermilion = Hex("DE7059");
        private readonly List<string> mTranscript = new();
        private GameObject mCanvasObject;
        private GameObject mBody;
        private Text mTitle;
        private Text mSubtitle;
        private Text mStatus;
        private AfterHoursGameModule mGame;
        private AfterHoursEvents mEvents;
        private IDisposable mTranscriptBinding;
        private IDisposable mScreenBinding;
        private int mRenderedRevision = -1;
        private float mTextScale = 1f;

        public UIRoot Root { get; private set; }

        public AfterHoursScreenController()
        {
            BuildCanvas();
        }

        public void Bind(AfterHoursGameModule game, AfterHoursEvents events)
        {
            mGame = game ?? throw new ArgumentNullException(nameof(game));
            mEvents = events ?? throw new ArgumentNullException(nameof(events));
            mTextScale = game.GetTextScale();
            mTranscriptBinding = events.TranscriptAdded.Bind(AddTranscript);
            mScreenBinding = events.ScreenChanged.Bind(_ => mRenderedRevision = -1);
            Render(true);
        }

        public void Tick()
        {
            if (mGame == null || mRenderedRevision == mGame.Revision)
                return;
            Render(false);
        }

        public void Dispose()
        {
            mTranscriptBinding?.Dispose();
            mScreenBinding?.Dispose();
            if (mCanvasObject != null)
                UnityEngine.Object.Destroy(mCanvasObject);
            Root = null;
        }

        private void BuildCanvas()
        {
            mCanvasObject = new GameObject("After Hours Field Interface",
                                           typeof(RectTransform), typeof(Canvas),
                                           typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UIRoot));
            UnityEngine.Object.DontDestroyOnLoad(mCanvasObject);
            var canvas = mCanvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = mCanvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            Root = mCanvasObject.GetComponent<UIRoot>();
            Root.Background = CreateLayer("Background");
            Root.Normal = CreateLayer("Normal");
            Root.Popup = CreateLayer("Popup");
            Root.Guide = CreateLayer("Guide");
            Root.Loading = CreateLayer("Loading");
            Root.System = CreateLayer("System");
        }

        private RectTransform CreateLayer(string name)
        {
            var layer = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            layer.SetParent(mCanvasObject.transform, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            return layer;
        }

        private void Render(bool force)
        {
            if (!force && mRenderedRevision == mGame.Revision)
                return;
            mRenderedRevision = mGame.Revision;
            Clear(Root.Normal);
            var shell = Panel(Root.Normal, "Night Office / Interface", Ink, Vector2.zero, Vector2.one,
                              new Vector2(30f, 24f), new Vector2(-30f, -24f));
            AddHeader(shell);
            mBody = Panel(shell.transform, "Screen body", Color.clear,
                          new Vector2(0f, 0.08f), new Vector2(1f, 0.86f),
                          new Vector2(0f, 0f), new Vector2(0f, 0f));
            var footer = Panel(shell.transform, "Footer", Hex("202C31"),
                               new Vector2(0f, 0f), new Vector2(1f, 0.075f),
                               Vector2.zero, Vector2.zero);
            switch (mGame.Screen)
            {
                case "investigation": DrawInvestigation(); break;
                case "encounter": DrawEncounter(); break;
                case "debrief": DrawDebrief(); break;
                case "manual": DrawManual(); break;
                case "settings": DrawSettings(); break;
                case "systems": DrawSystemsLab(); break;
                case "data": DrawDataWorkshop(); break;
                case "party": DrawParty(); break;
                case "archive": DrawArchive(); break;
                default: DrawOffice(); break;
            }
            mStatus = Label(footer.transform, "status", Tr("status.office", "FIELD OFFICE  /  CITY DISTRICT 03     •     CASE FILE 01 / THE LAST STOP"),
                            14, Muted, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0.72f, 1f),
                            new Vector2(22f, 0f), new Vector2(0f, 0f));
            Label(footer.transform, "controls", Tr("status.controls", "TAB  FIELD MANUAL     F3  DIAGNOSTICS     ESC  RETURN"),
                  12, Cyan, TextAnchor.MiddleRight, new Vector2(0.68f, 0f), Vector2.one,
                  new Vector2(0f, 0f), new Vector2(-22f, 0f));
        }

        private void AddHeader(GameObject shell)
        {
            var header = Panel(shell.transform, "Masthead", Hex("202C31"),
                               new Vector2(0f, 0.87f), Vector2.one,
                               Vector2.zero, Vector2.zero);
            Text headerMark = Label(header.transform, "mark", "NIGHT\nOFFICE", 20, Paper,
                                    TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(0.105f, 1f),
                                    new Vector2(24f, 0f), new Vector2(0f, 0f));
            headerMark.fontStyle = FontStyle.Bold;
            Label(header.transform, "separator", "//", 18, Vermilion, TextAnchor.MiddleCenter,
                  new Vector2(0.103f, 0f), new Vector2(0.13f, 1f), Vector2.zero, Vector2.zero);
            mTitle = Label(header.transform, "title", "AFTER HOURS", 24, Paper, TextAnchor.MiddleLeft,
                           new Vector2(0.14f, 0.43f), new Vector2(0.62f, 1f), Vector2.zero, Vector2.zero);
            mTitle.fontStyle = FontStyle.Bold;
            mSubtitle = Label(header.transform, "subtitle", "CASE 01  /  THE LAST STOP", 12, Cyan,
                              TextAnchor.MiddleLeft, new Vector2(0.14f, 0f), new Vector2(0.62f, 0.46f),
                              Vector2.zero, Vector2.zero);

            Button(header.transform, "Field manual", Tr("nav.manual", "FIELD MANUAL"), () => mGame.OpenManual(),
                   new Vector2(0.69f, 0.21f), new Vector2(0.81f, 0.79f), false);
            Button(header.transform, "Systems lab", Tr("nav.systems", "SYSTEMS LAB"), () => mGame.OpenSystemsLab(),
                   new Vector2(0.82f, 0.21f), new Vector2(0.93f, 0.79f), false);
            Button(header.transform, "Settings", Tr("nav.settings", "SETTINGS"), () => mGame.OpenSettings(),
                   new Vector2(0.94f, 0.21f), new Vector2(1f, 0.79f), false);
        }

        private void AddFooter(Transform parent)
        {
            mStatus = Label(parent, "status", "", 14, Muted, TextAnchor.MiddleLeft,
                            new Vector2(0f, 0f), new Vector2(0.72f, 1f),
                            new Vector2(22f, 0f), Vector2.zero);
            Label(parent, "controls", "TAB  FIELD MANUAL     F3  DIAGNOSTICS     ESC  RETURN",
                  12, Cyan, TextAnchor.MiddleRight, new Vector2(0.68f, 0f), Vector2.one,
                  Vector2.zero, new Vector2(-22f, 0f));
        }

        private void DrawOffice()
        {
            SetHeader(Tr("office.title", "THE NIGHT SHIFT"), Tr("office.subtitle", "A CITY CASEWORK RPG  /  OFFLINE STORY PROFILE"));
            var hero = Panel(mBody.transform, "Office feature", Hex("253238"),
                             new Vector2(0.035f, 0.055f), new Vector2(0.965f, 0.955f),
                             Vector2.zero, Vector2.zero);
            AddCityGrid(Panel(hero.transform, "City contour", Color.clear,
                new Vector2(0.36f, 0.02f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero));
            Label(hero.transform, "kicker", Tr("office.kicker", "MUNICIPAL ANOMALY BUREAU     /     NIGHT WATCH 04"), 14, Cyan,
                  TextAnchor.MiddleLeft, new Vector2(0.06f, 0.79f), new Vector2(0.7f, 0.88f), Vector2.zero, Vector2.zero);
            var title = Label(hero.transform, "hero title", Tr("office.headline", "Somewhere,\nthe last tram is waiting."), 44, Paper,
                              TextAnchor.MiddleLeft, new Vector2(0.06f, 0.42f), new Vector2(0.67f, 0.81f), Vector2.zero, Vector2.zero);
            title.fontStyle = FontStyle.Bold;
            Label(hero.transform, "hero copy", Tr("office.body", "At 01:17 a route that no longer exists appears on the northbound line.\nFind the stop. Speak to the people who remember it. Bring your team home."),
                  17, Muted, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.28f), new Vector2(0.69f, 0.44f), Vector2.zero, Vector2.zero);
            var rank = mGame.Profile.Rank;
            Label(hero.transform, "profile", $"FIELD AGENT  /  {mGame.Profile.Callsign.ToUpperInvariant()}     •     RANK {rank:00}     •     {mGame.Profile.Experience:000} XP     •     SIGNAL SHARDS {mGame.Profile.SignalShards:00}",
                  13, Paper, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.17f), new Vector2(0.7f, 0.25f), Vector2.zero, Vector2.zero);
            Button(hero.transform, "Open case", Tr("office.open", "OPEN CASE FILE     →"), () => _ = mGame.BeginCaseAsync(),
                   new Vector2(0.06f, 0.045f), new Vector2(0.28f, 0.15f), true);
            Button(hero.transform, "Party file", "FIELD TEAM", mGame.OpenParty,
                   new Vector2(0.3f, 0.045f), new Vector2(0.47f, 0.15f), false);
            Button(hero.transform, "Archive file", "CASE ARCHIVE", mGame.OpenArchive,
                   new Vector2(0.49f, 0.045f), new Vector2(0.68f, 0.15f), false);
            var archive = Panel(mBody.transform, "Archive index", Hex("202C31"),
                                new Vector2(0.035f, -0.015f), new Vector2(0.44f, 0.03f),
                                Vector2.zero, Vector2.zero);
            Label(archive.transform, "archive label", $"ARCHIVE  /  {mGame.Profile.ArchiveEntries.Count:00} ENTRIES     •     RESOLVED FILES {mGame.Profile.ResolvedCases.Count:00}",
                  12, Muted, TextAnchor.MiddleLeft, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), Vector2.zero, Vector2.zero);
        }

        private void DrawInvestigation()
        {
            SetHeader(Tr("case.title", "FIELD CASE / LAST STOP"), Tr("case.subtitle", "DISTRICT 03     •     MOVE WITH A / D OR ARROWS     •     Q TO INSPECT"));
            var map = Panel(mBody.transform, "Neighborhood map", Hex("202C31"),
                            new Vector2(0.025f, 0.07f), new Vector2(0.64f, 0.93f), Vector2.zero, Vector2.zero);
            AddCityGrid(Panel(map.transform, "Map art", Color.clear,
                new Vector2(0.03f, 0.14f), new Vector2(0.97f, 0.93f), Vector2.zero, Vector2.zero));
            Label(map.transform, "map title", Tr("case.map", "NORTHBOUND / RAIN DISTRICT"), 14, Cyan,
                  TextAnchor.MiddleLeft, new Vector2(0.05f, 0.93f), new Vector2(0.75f, 1f), Vector2.zero, Vector2.zero);
            string[] places = { Tr("place.clerk", "NIGHT CLERK"), Tr("place.ticket", "TICKET WINDOW"), Tr("place.tram", "OLD TRAM"), Tr("place.platform", "PLATFORM ZERO") };
            for (int i = 0; i < places.Length; i++)
            {
                int place = i;
                float x = 0.09f + i * 0.27f;
                var marker = Button(map.transform, "Place " + i, $"{(i + 1):00}\n{places[i]}",
                                    () => { mGame.MoveLocation(place - mGame.Session.SelectedLocation); mGame.Inspect(); },
                                    new Vector2(x, 0.08f), new Vector2(x + 0.22f, 0.28f),
                                    i == mGame.Session.SelectedLocation);
                marker.GetComponent<Image>().color = i == mGame.Session.SelectedLocation ? Hex("305056") : Hex("28363A");
            }
            var dossier = Panel(mBody.transform, "Dossier", Hex("E2DDD2"),
                                new Vector2(0.665f, 0.07f), new Vector2(0.975f, 0.93f), Vector2.zero, Vector2.zero);
            Label(dossier.transform, "dossier label", Tr("case.dossier", "EVIDENCE DOSSIER     /     01"), 12, Hex("47615F"), TextAnchor.MiddleLeft,
                  new Vector2(0.07f, 0.89f), new Vector2(0.93f, 0.97f), Vector2.zero, Vector2.zero);
            Label(dossier.transform, "dossier headline", Tr("case.headline", "The stop\nthat stayed."), 27, Ink, TextAnchor.MiddleLeft,
                  new Vector2(0.07f, 0.71f), new Vector2(0.93f, 0.89f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            Label(dossier.transform, "clue count", $"LOGGED  {mGame.Session.ClueCount:00} / 02     •     SIGNAL  {mGame.Session.SignalReserve:00}",
                  13, Hex("47615F"), TextAnchor.MiddleLeft, new Vector2(0.07f, 0.63f), new Vector2(0.93f, 0.7f), Vector2.zero, Vector2.zero);
            Label(dossier.transform, "play guide", Tr("case.playguide", "CLUES / Inspect 02 + 04. Click markers or press Q."),
                  14, Hex("334B49"), TextAnchor.UpperLeft, new Vector2(0.07f, 0.52f), new Vector2(0.93f, 0.61f), Vector2.zero, Vector2.zero);
            Label(dossier.transform, "recent notes", mGame.GetSelectedLocationNote() + "\n\n" + RecentTranscript(), 13, Ink, TextAnchor.UpperLeft,
                  new Vector2(0.07f, 0.27f), new Vector2(0.93f, 0.50f), Vector2.zero, Vector2.zero);
            Button(dossier.transform, "interview", Tr("case.interview", "READ RESIDENT INTERVIEW"), mGame.ReadResidentInterview,
                   new Vector2(0.07f, 0.16f), new Vector2(0.93f, 0.245f), false);
            Button(dossier.transform, "enter platform", "ENTER PLATFORM ZERO  →", () =>
            {
                if (!mGame.StartEncounter()) AddTranscript("CASE BOARD / TWO EVIDENCE SOURCES REQUIRED BEFORE CONTACT.");
            }, new Vector2(0.07f, 0.05f), new Vector2(0.93f, 0.14f), true).GetComponent<Button>().interactable = mGame.Session.ClueCount >= 2;
        }

        private void DrawEncounter()
        {
            SetHeader(Tr("combat.title", "TACTICAL CONTACT"), Tr("combat.subtitle", "TURN-BASED ENCOUNTER     •     CHOOSE A SKILL     •     WATCH YOUR SIGNAL RESERVE"));
            var field = Panel(mBody.transform, "Encounter field", Hex("202C31"),
                              new Vector2(0.025f, 0.07f), new Vector2(0.66f, 0.93f), Vector2.zero, Vector2.zero);
            Label(field.transform, "combat caption", "PLATFORM ZERO  /  01:24", 13, Cyan, TextAnchor.MiddleLeft,
                  new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
            int partyOrdinal = 0;
            for (int i = 0; i < mGame.Encounter.Actors.Count; i++)
            {
                EncounterActor actor = mGame.Encounter.Actors[i];
                float x = actor.IsEnemy ? 0.69f : 0.08f + partyOrdinal++ * 0.28f;
                float y = actor.IsEnemy ? 0.33f : 0.25f;
                var card = Panel(field.transform, "Actor " + actor.Id, actor.IsEnemy ? Hex("4D3535") : Hex("33474A"),
                                 new Vector2(x, y), new Vector2(x + 0.22f, y + 0.4f), Vector2.zero, Vector2.zero);
                Label(card.transform, "actor name", actor.Name.ToUpperInvariant(), 13, actor.IsEnemy ? Vermilion : Paper,
                      TextAnchor.MiddleCenter, new Vector2(0f, 0.68f), Vector2.one, Vector2.zero, Vector2.zero);
                Label(card.transform, "actor hp", $"HP  {actor.Health:00} / {actor.MaxHealth:00}\nFOCUS  {actor.Focus:00}", 13, Muted,
                      TextAnchor.MiddleCenter, new Vector2(0f, 0.2f), new Vector2(1f, 0.7f), Vector2.zero, Vector2.zero);
                if (actor.Id == mGame.Encounter.CurrentActor?.Id)
                    Label(card.transform, "turn", "ACTIVE TURN", 10, Cyan, TextAnchor.MiddleCenter,
                          new Vector2(0f, 0.01f), new Vector2(1f, 0.2f), Vector2.zero, Vector2.zero);
            }
            var action = Panel(mBody.transform, "Tactical actions", Paper,
                               new Vector2(0.685f, 0.07f), new Vector2(0.975f, 0.93f), Vector2.zero, Vector2.zero);
            Label(action.transform, "turn status", $"ROUND  {mGame.Encounter.Round:00}     /     SIGNAL  {mGame.Session.SignalReserve:00}", 12, Hex("47615F"),
                  TextAnchor.MiddleLeft, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);
            Label(action.transform, "skill heading", Tr("combat.actions", "FIELD ACTIONS"), 22, Ink, TextAnchor.MiddleLeft,
                  new Vector2(0.08f, 0.79f), new Vector2(0.92f, 0.87f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            AddSkill(action.transform, Tr("combat.skill.basic", "01   BASIC STRIKE"), "No signal cost / steady damage", "basic-strike", 0.65f, false);
            AddSkill(action.transform, Tr("combat.skill.counter", "02   COUNTER-SIGNAL"), "Spend 3 signal / heavy impact", "counter-signal", 0.49f, true);
            AddSkill(action.transform, Tr("combat.skill.hold", "03   HOLD THE LINE"), "Spend 2 signal / disrupt intent", "hold-the-line", 0.33f, false);
            AddSkill(action.transform, Tr("combat.skill.echo", "04   ECHO STRIKE"), "A focused follow-up", "echo-strike", 0.17f, false);
            Button(action.transform, "Withdraw", Tr("combat.withdraw", "WITHDRAW TO DEBRIEF"), () => mGame.ResolveCase(false),
                   new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.12f), false);
        }

        private void AddSkill(Transform parent, string title, string copy, string id, float y, bool accent)
        {
            Button(parent, title, title + "\n" + copy, () => mGame.UseSkill(id),
                   new Vector2(0.08f, y), new Vector2(0.92f, y + 0.125f), accent)
                   .GetComponent<Button>().interactable = mGame.Encounter.CurrentActor != null &&
                                                          !mGame.Encounter.CurrentActor.IsEnemy;
        }

        private void DrawDebrief()
        {
            SetHeader(Tr("debrief.title", "CASE DEBRIEF"), "SHIFT REVIEW     /     FILE 01 — THE LAST STOP");
            var report = Panel(mBody.transform, "Report", Paper,
                               new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);
            bool success = mGame.Session.Outcome == CaseOutcome.Resolved;
            Label(report.transform, "stamp", Tr(success ? "debrief.success" : "debrief.failure", success ? "RESOLVED     /     CIVILIAN ROUTE RESTORED" : "WITHDRAWN     /     CASE REMAINS OPEN"), 13,
                  success ? Hex("47615F") : Hex("A64E3C"), TextAnchor.MiddleLeft,
                  new Vector2(0.08f, 0.82f), new Vector2(0.88f, 0.93f), Vector2.zero, Vector2.zero);
            Label(report.transform, "report title", success ? "The platform\nfinally emptied." : "The night\nis not over.", 42, Ink,
                  TextAnchor.MiddleLeft, new Vector2(0.08f, 0.52f), new Vector2(0.88f, 0.84f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            Label(report.transform, "summary", $"Evidence logged: {mGame.Session.ClueCount:00}\nReward: {(success ? "+40 XP     +1 signal shard     +1 archive entry" : "No reward issued for an unresolved file")}\n\n{RecentTranscript()}",
                  16, Hex("455450"), TextAnchor.UpperLeft, new Vector2(0.08f, 0.21f), new Vector2(0.85f, 0.5f), Vector2.zero, Vector2.zero);
            Button(report.transform, "Return", Tr("debrief.return", "RETURN TO NIGHT OFFICE     →"), () => _ = mGame.ReturnToOfficeAsync(),
                   new Vector2(0.08f, 0.075f), new Vector2(0.48f, 0.18f), true);
        }

        private void DrawManual()
        {
            SetHeader(Tr("nav.manual", "FIELD MANUAL"), "A WORKING INDEX OF THE FRAMEWORK     /     PRESS TAB TO RETURN");
            var sheet = Panel(mBody.transform, "Feature index", Paper,
                              new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);
            Label(sheet.transform, "manual title", "Systems behind the story.", 30, Ink, TextAnchor.MiddleLeft,
                  new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            string[] entries = {
                "01  BOOTSTRAP / dependency graph / module scopes / diagnostics / application lifecycle",
                "02  STORY / typed flows / application + scene models / case transitions / event ownership",
                "03  FIELD / entity components / ordered signal systems / turn rules / pooled case notes",
                "04  SERVICES / saves / settings / localization / compiled table contracts / named input",
                "05  PRESENTATION / responsive UGUI / vector map / event transcript / audio capability",
                "06  OPTIONAL LABS / Addressables / AssetBundles / content updates / generated relay protocol",
                "07  AUTHORING / schema inputs / generated message + row types / package sample validation"
            };
            for (int i = 0; i < entries.Length; i++)
            {
                float top = 0.79f - i * 0.105f;
                Label(sheet.transform, "entry " + i, entries[i], 14, Ink, TextAnchor.MiddleLeft,
                      new Vector2(0.06f, top - 0.09f), new Vector2(0.95f, top), Vector2.zero, Vector2.zero);
            }
            Button(sheet.transform, "Manual back", Tr("nav.back", "BACK TO OFFICE"), () => mGame.ReturnToOfficeScreen(),
                   new Vector2(0.06f, 0.04f), new Vector2(0.3f, 0.12f), true);
            Button(sheet.transform, "Sample Hub", Tr("hub.return", "BACK TO SAMPLE HUB"), AfterHoursBootstrap.ReturnToSampleHub,
                   new Vector2(0.68f, 0.04f), new Vector2(0.95f, 0.12f), false);
        }

        private void DrawSettings()
        {
            SetHeader(Tr("nav.settings", "FIELD SETTINGS"), "PROFILE PREFERENCES ARE SAVED LOCALLY");
            var sheet = Panel(mBody.transform, "Settings card", Paper,
                              new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.9f), Vector2.zero, Vector2.zero);
            Label(sheet.transform, "settings title", Tr("settings.title", "Make the night readable."), 30, Ink, TextAnchor.MiddleLeft,
                  new Vector2(0.08f, 0.78f), new Vector2(0.9f, 0.94f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            Label(sheet.transform, "language label", Tr("settings.language", "LANGUAGE  /  ENGLISH • 简体中文 • 日本語"), 15, Ink, TextAnchor.MiddleLeft,
                  new Vector2(0.08f, 0.66f), new Vector2(0.9f, 0.74f), Vector2.zero, Vector2.zero);
            Button(sheet.transform, "English", "ENGLISH", () => mGame.SetLanguage("en-US"),
                   new Vector2(0.08f, 0.55f), new Vector2(0.32f, 0.64f), mGame.Profile.Language == "en-US");
            Button(sheet.transform, "Chinese", "简体中文", () => mGame.SetLanguage("zh-CN"),
                   new Vector2(0.36f, 0.55f), new Vector2(0.6f, 0.64f), mGame.Profile.Language == "zh-CN");
            Button(sheet.transform, "Japanese", "日本語", () => mGame.SetLanguage("ja-JP"),
                   new Vector2(0.64f, 0.55f), new Vector2(0.88f, 0.64f), mGame.Profile.Language == "ja-JP");
            Label(sheet.transform, "type scale label", Tr("settings.size", "TEXT SIZE"), 14, Ink, TextAnchor.MiddleLeft,
                  new Vector2(0.08f, 0.41f), new Vector2(0.3f, 0.49f), Vector2.zero, Vector2.zero);
            Button(sheet.transform, "Smaller text", "A−", () => mGame.SetTextScale(mTextScale - 0.1f),
                   new Vector2(0.36f, 0.4f), new Vector2(0.48f, 0.5f), false);
            Label(sheet.transform, "scale value", $"{mTextScale:0.0}×", 15, Ink, TextAnchor.MiddleCenter,
                  new Vector2(0.49f, 0.4f), new Vector2(0.58f, 0.5f), Vector2.zero, Vector2.zero);
            Button(sheet.transform, "Larger text", "A+", () => mGame.SetTextScale(mTextScale + 0.1f),
                   new Vector2(0.59f, 0.4f), new Vector2(0.71f, 0.5f), false);
            Label(sheet.transform, "volume label", "EFFECTS VOLUME", 14, Ink, TextAnchor.MiddleLeft,
                  new Vector2(0.08f, 0.27f), new Vector2(0.32f, 0.36f), Vector2.zero, Vector2.zero);
            AddVolumeSlider(sheet.transform);
            Label(sheet.transform, "save note", "The story is playable offline. Your profile, language, and accessibility settings stay on this device.",
                  15, Hex("47615F"), TextAnchor.MiddleLeft, new Vector2(0.08f, 0.16f), new Vector2(0.88f, 0.25f), Vector2.zero, Vector2.zero);
            Button(sheet.transform, "Settings back", Tr("nav.back", "BACK TO OFFICE"), () => mGame.ReturnToOfficeScreen(),
                   new Vector2(0.08f, 0.06f), new Vector2(0.32f, 0.15f), true);
        }

        private void DrawSystemsLab()
        {
            SetHeader(Tr("nav.systems", "SYSTEMS LAB"), "OPT-IN TECHNICAL DEMONSTRATIONS     /     STORY PROFILE REMAINS OFFLINE");
            var sheet = Panel(mBody.transform, "Systems workbench", Hex("253238"),
                              new Vector2(0.035f, 0.055f), new Vector2(0.965f, 0.945f), Vector2.zero, Vector2.zero);
            Label(sheet.transform, "lab title", "A framework, under the surface.", 30, Paper, TextAnchor.MiddleLeft,
                  new Vector2(0.05f, 0.82f), new Vector2(0.68f, 0.95f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            Button(sheet.transform, "Open managed window", "OPEN TRITONE WINDOW",
                   mGame.OpenFrameworkWindow,
                   new Vector2(0.7f, 0.84f), new Vector2(0.95f, 0.94f), false);
            string[] items = {
                "LIVE RUNTIME  /  F3 opens frame timings, active module and flow, entity counts, and recent logs.",
                "SCENE LIFETIME  /  switch Office, Case and Systems Lab modules while the field interface persists.",
                "SIGNAL SYSTEM  /  ordered ECS presentation advances evidence pulses and enemy intent cues.",
                "OWNED SERVICES  /  try one-shot timers, eased tweens, main-thread dispatch, pooling and entity updates below."
            };
            for (int i = 0; i < items.Length; i++)
            {
                float top = 0.7f - i * 0.14f;
                Label(sheet.transform, "system " + i, $"{(i + 1):00}   /   {items[i]}", 14,
                      Paper, TextAnchor.MiddleLeft,
                      new Vector2(0.06f, top - 0.09f), new Vector2(0.94f, top), Vector2.zero, Vector2.zero);
            }
            Label(sheet.transform, "relay status", "PROFILE SERVICES  /  " + mGame.RelayStatus, 12,
                  mGame.HasNetworkProfile ? Cyan : Muted, TextAnchor.MiddleLeft,
                  new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.28f), Vector2.zero, Vector2.zero);
            Button(sheet.transform, "Run timer", "RUN TIMER", () => mGame.RunSystemsProbe("timer"),
                   new Vector2(0.06f, 0.09f), new Vector2(0.26f, 0.2f), true);
            Button(sheet.transform, "Run tween", "RUN TWEEN", () => mGame.RunSystemsProbe("tween"),
                   new Vector2(0.29f, 0.09f), new Vector2(0.49f, 0.2f), false);
            Button(sheet.transform, "Dispatch pool", "DISPATCH + POOL", () => mGame.RunSystemsProbe("dispatch-pool"),
                   new Vector2(0.52f, 0.09f), new Vector2(0.74f, 0.2f), false);
            Button(sheet.transform, "Spawn entity", "SPAWN ENTITY", () => mGame.RunSystemsProbe("entity"),
                   new Vector2(0.77f, 0.09f), new Vector2(0.94f, 0.2f), false);
            Button(sheet.transform, "Load case data", "LOAD CASE DATA",
                   () => _ = mGame.RunAssetServiceProbeAsync(),
                   new Vector2(0.06f, 0.025f), new Vector2(0.29f, 0.085f), false);
            if (mGame.HasNetworkProfile)
                Button(sheet.transform, "Relay connect", "CONNECT LOCAL RELAY",
                       () => _ = mGame.ConnectLocalRelayAsync(),
                       new Vector2(0.32f, 0.025f), new Vector2(0.61f, 0.085f), false);
            else if (mGame.HasAddressablesProfile || mGame.HasContentUpdateProfile)
                Button(sheet.transform, "Asset delivery", mGame.HasContentUpdateProfile ? "CHECK / APPLY CONTENT" : "CATALOG / PREFETCH",
                       () => _ = mGame.RunAssetDeliveryProbeAsync(),
                       new Vector2(0.32f, 0.025f), new Vector2(0.61f, 0.085f), false);
            Button(sheet.transform, "Lab back", Tr("nav.back", "BACK TO OFFICE"), () => mGame.ReturnToOfficeScreen(),
                   new Vector2(0.61f, 0.025f), new Vector2(0.77f, 0.085f), false);
            Button(sheet.transform, "Sample Hub", Tr("hub.return", "BACK TO SAMPLE HUB"), AfterHoursBootstrap.ReturnToSampleHub,
                   new Vector2(0.79f, 0.025f), new Vector2(0.95f, 0.085f), false);
        }

        private void DrawDataWorkshop()
        {
            SetHeader(Tr("data.title", "DATA WORKSHOP"), Tr("data.subtitle", "TYPED RUNTIME TABLES / LOADED BY TRITONE"));
            var sheet = Panel(mBody.transform, "Typed table workbench", Hex("253238"),
                              new Vector2(0.035f, 0.055f), new Vector2(0.965f, 0.945f), Vector2.zero, Vector2.zero);
            Label(sheet.transform, "data intro", Tr("data.intro", "Authored clues become live case records."), 28, Paper,
                  TextAnchor.MiddleLeft, new Vector2(0.05f, 0.83f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            Label(sheet.transform, "data path", Tr("data.path", "CSV  →  SCHEMA  →  GENERATED C# ROW  →  TRITONE TABLE SERVICE  →  RPG CASE"),
                  13, Cyan, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.83f), Vector2.zero, Vector2.zero);

            var clues = Panel(sheet.transform, "Loaded clue rows", Hex("E2DDD2"),
                              new Vector2(0.05f, 0.29f), new Vector2(0.61f, 0.70f), Vector2.zero, Vector2.zero);
            Label(clues.transform, "clue heading", string.Format(Tr("data.clues", "CLUES  /  {0} ROWS  /  ClueRow"), mGame.ClueTableCount),
                  15, Hex("47615F"), TextAnchor.MiddleLeft, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            for (int i = 0; i < mGame.ClueTableCount; i++)
            {
                float top = 0.76f - i * (0.62f / Mathf.Max(1, mGame.ClueTableCount));
                Label(clues.transform, "clue row " + i, mGame.GetClueTableRow(i), 13, Ink,
                      TextAnchor.MiddleLeft, new Vector2(0.05f, top - 0.25f), new Vector2(0.95f, top), Vector2.zero, Vector2.zero);
            }

            var skills = Panel(sheet.transform, "Loaded skill rows", Hex("202C31"),
                               new Vector2(0.63f, 0.29f), new Vector2(0.95f, 0.70f), Vector2.zero, Vector2.zero);
            Label(skills.transform, "skill heading", string.Format(Tr("data.skills", "SKILLS  /  {0} ROWS  /  SkillRow"), mGame.SkillTableCount),
                  14, Cyan, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.98f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            for (int i = 0; i < mGame.SkillTableCount; i++)
            {
                float top = 0.76f - i * (0.62f / Mathf.Max(1, mGame.SkillTableCount));
                Label(skills.transform, "skill row " + i, mGame.GetSkillTableRow(i), 12, Paper,
                      TextAnchor.MiddleLeft, new Vector2(0.06f, top - 0.12f), new Vector2(0.94f, top), Vector2.zero, Vector2.zero);
            }

            Label(sheet.transform, "authoring path", "Data/clues.csv  ·  Schemas/Tables.json  ·  Runtime/Generated/Tables  ·  Resources/Tables\n" +
                  Tr("data.validator", "CUSTOM VALIDATOR  /  Clue tables require at least 2 evidence rows before output commit."),
                  12, Muted, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.27f), Vector2.zero, Vector2.zero);
            Button(sheet.transform, "Start table-backed case", Tr("data.openCase", "OPEN CASE USING THESE TABLES  →"),
                   () => _ = mGame.BeginCaseAsync(), new Vector2(0.05f, 0.045f), new Vector2(0.55f, 0.13f), true);
            Button(sheet.transform, "Sample Hub", Tr("hub.return", "BACK TO SAMPLE HUB"), AfterHoursBootstrap.ReturnToSampleHub,
                   new Vector2(0.68f, 0.045f), new Vector2(0.95f, 0.13f), false);
            Label(sheet.transform, "authoring menu", Tr("data.menu", "UNITY EDITOR  /  Tritone  /  After Hours  /  Regenerate Table Outputs"),
                  11, Cyan, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.005f), new Vector2(0.95f, 0.045f), Vector2.zero, Vector2.zero);
        }

        private void DrawParty()
        {
            SetHeader("FIELD TEAM", "NIGHT WATCH 04     /     ACTIVE ROSTER");
            var sheet = Panel(mBody.transform, "Team dossier", Paper,
                              new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);
            Label(sheet.transform, "team heading", $"AGENT {mGame.Profile.Callsign.ToUpperInvariant()}     /     RANK {mGame.Profile.Rank:00}     /     {mGame.Profile.Experience:000} XP",
                  26, Ink, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            DrawOperatorCard(sheet.transform, "MORROW", "FIELD LEAD / SIGNAL HANDLER", "HP  26    FOCUS  08", "COUNTER-SIGNAL  /  HOLD THE LINE", 0.06f);
            DrawOperatorCard(sheet.transform, "ROOK", "CLOSE SUPPORT / ROUTE SPECIALIST", "HP  22    FOCUS  09", "ECHO STRIKE  /  BASIC STRIKE", 0.52f);
            Label(sheet.transform, "team note", "Operators share the shift, but each takes a distinct turn. Signal skills consume focus and can interrupt an enemy action.",
                  15, Hex("47615F"), TextAnchor.MiddleLeft, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.25f), Vector2.zero, Vector2.zero);
            Button(sheet.transform, "Team back", Tr("nav.back", "BACK TO OFFICE"), () => mGame.ReturnToOfficeScreen(),
                   new Vector2(0.06f, 0.04f), new Vector2(0.32f, 0.13f), true);
        }

        private void DrawOperatorCard(Transform parent, string callsign, string role, string stats, string skills, float x)
        {
            var card = Panel(parent, callsign, Hex("D5D0C5"),
                             new Vector2(x, 0.33f), new Vector2(x + 0.42f, 0.77f), Vector2.zero, Vector2.zero);
            Label(card.transform, "callsign", callsign, 25, Ink, TextAnchor.MiddleLeft,
                  new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            Label(card.transform, "role", role, 12, Hex("47615F"), TextAnchor.MiddleLeft,
                  new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.68f), Vector2.zero, Vector2.zero);
            Label(card.transform, "stats", stats, 16, Ink, TextAnchor.MiddleLeft,
                  new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.5f), Vector2.zero, Vector2.zero);
            Label(card.transform, "skills", skills, 13, Hex("47615F"), TextAnchor.MiddleLeft,
                  new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.29f), Vector2.zero, Vector2.zero);
        }

        private void DrawArchive()
        {
            SetHeader("CASE ARCHIVE", "RECORDS RETAINED LOCALLY     /     NIGHT OFFICE 04");
            var sheet = Panel(mBody.transform, "Case archive", Paper,
                              new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);
            Label(sheet.transform, "archive heading", "FILE 01     /     THE LAST STOP", 26, Ink, TextAnchor.MiddleLeft,
                  new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            bool resolved = mGame.Profile.ResolvedCases.Contains("last-stop");
            Label(sheet.transform, "archive status", resolved ? "CLOSED     /     PASSENGER ROUTE RECONCILED" : "OPEN     /     READY FOR FIELD INVESTIGATION", 14,
                  resolved ? Hex("47615F") : Hex("A64E3C"), TextAnchor.MiddleLeft,
                  new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.8f), Vector2.zero, Vector2.zero);
            Label(sheet.transform, "archive narrative", "01:17 — The northbound tram arrives at a stop missing from every municipal map.\n\nEVIDENCE\n• Brass token: route identifier removed from 1986 transit records.\n• Platform Zero: upward rain, warm timetable, tomorrow's date.\n\nPEOPLE\n• Night clerk remembers a passenger who paid with a retired-line token.\n\nOUTCOME\n" + (resolved ? "The route has been released. One signal shard was added to the case shelf." : "No outcome recorded. Resolve the field case to complete this entry."),
                  16, Ink, TextAnchor.UpperLeft, new Vector2(0.06f, 0.23f), new Vector2(0.94f, 0.69f), Vector2.zero, Vector2.zero);
            Button(sheet.transform, "Archive back", Tr("nav.back", "BACK TO OFFICE"), () => mGame.ReturnToOfficeScreen(),
                   new Vector2(0.06f, 0.06f), new Vector2(0.32f, 0.15f), true);
        }

        private void SetHeader(string title, string subtitle)
        {
            if (mTitle != null) mTitle.text = title;
            if (mSubtitle != null) mSubtitle.text = subtitle;
        }

        private string Tr(string key, string fallback)
        {
            if (mGame == null) return fallback;
            string value = mGame.GetLocalizedText(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }

        private void AddVolumeSlider(Transform parent)
        {
            var root = Panel(parent, "Effects volume slider", Hex("B8B2A5"),
                             new Vector2(0.36f, 0.285f), new Vector2(0.88f, 0.345f), Vector2.zero, Vector2.zero);
            var fill = Panel(root.transform, "Fill", Hex("527E7C"), Vector2.zero, new Vector2(0.7f, 1f), Vector2.zero, Vector2.zero);
            var slider = root.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(mGame.GetSoundVolume());
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = root.GetComponent<Image>();
            slider.onValueChanged.AddListener(mGame.SetSoundVolume);
        }

        private void AddTranscript(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            mTranscript.Add(value);
            if (mTranscript.Count > 12) mTranscript.RemoveAt(0);
            if (mGame != null) mRenderedRevision = -1;
        }

        private string RecentTranscript()
        {
            if (mTranscript.Count == 0)
                return "01:17  /  NORTHBOUND ROUTE NOT FOUND ON CITY MAP\n01:19  /  NIGHT CLERK REQUESTED A QUIET INTERVIEW\n\nTwo independent clues will open the platform approach.";
            int start = Mathf.Max(0, mTranscript.Count - 5);
            return string.Join("\n", mTranscript.GetRange(start, mTranscript.Count - start).ToArray());
        }

        private static GameObject Panel(Transform parent, string name, Color color,
                                       Vector2 anchorMin, Vector2 anchorMax,
                                       Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private Text Label(Transform parent, string name, string value, int size, Color color,
                                  TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax,
                                  Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.RoundToInt(size * mTextScale);
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            text.text = value;
            return text;
        }

        private GameObject Button(Transform parent, string name, string caption, UnityAction callback,
                                         Vector2 anchorMin, Vector2 anchorMax, bool primary)
        {
            var go = Panel(parent, name, primary ? Hex("527E7C") : Hex("314147"),
                           anchorMin, anchorMax, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = primary ? Hex("527E7C") : Hex("314147");
            colors.highlightedColor = primary ? Hex("6AA19C") : Hex("496065");
            colors.pressedColor = Vermilion;
            colors.selectedColor = Cyan;
            colors.disabledColor = Hex("697273");
            button.colors = colors;
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(callback);
            var captionText = Label(go.transform, "Label", caption, 13, primary ? Paper : Paper,
                                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one,
                                    new Vector2(8f, 4f), new Vector2(-8f, -4f));
            captionText.fontStyle = primary ? FontStyle.Bold : FontStyle.Normal;
            return go;
        }

        private static void Clear(RectTransform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
        }

        private static void AddCityGrid(GameObject target)
        {
            var graphicObject = new GameObject("City grid", typeof(RectTransform), typeof(CanvasRenderer), typeof(CityGridGraphic));
            graphicObject.transform.SetParent(target.transform, false);
            graphicObject.GetComponent<CityGridGraphic>().Attach(target.GetComponent<RectTransform>());
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var color) ? color : Color.magenta;

        /// <summary>Draws a restrained city plan without external art or textures.</summary>
        private sealed class CityGridGraphic : Graphic
        {
            private static readonly Vector2[] RoadA = { new(0.04f, 0.24f), new(0.22f, 0.55f), new(0.44f, 0.45f), new(0.62f, 0.72f), new(0.95f, 0.65f) };
            private static readonly Vector2[] RoadB = { new(0.11f, 0.91f), new(0.3f, 0.72f), new(0.51f, 0.76f), new(0.68f, 0.39f), new(0.89f, 0.24f) };
            public void Attach(RectTransform parent)
            {
                transform.SetParent(parent, false);
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                color = Color.white;
                raycastTarget = false;
                SetAllDirty();
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                Rect rect = GetPixelAdjustedRect();
                Color block = new(0.72f, 0.79f, 0.75f, 0.09f);
                for (int i = 0; i < 15; i++)
                {
                    float x = (i * 0.173f + 0.035f) % 0.94f;
                    float y = (i * 0.297f + 0.08f) % 0.87f;
                    float w = 0.1f + (i % 3) * 0.025f;
                    float h = 0.06f + (i % 4) * 0.02f;
                    Quad(vh, rect, new Rect(x, y, Mathf.Min(w, 0.98f - x), Mathf.Min(h, 0.96f - y)), block);
                }
                Polyline(vh, rect, RoadA, new Color(Cyan.r, Cyan.g, Cyan.b, 0.6f), 3f);
                Polyline(vh, rect, RoadB, new Color(Paper.r, Paper.g, Paper.b, 0.18f), 2f);
                Dot(vh, rect, RoadA[1], 7f, Vermilion);
                Dot(vh, rect, RoadA[3], 7f, Cyan);
                Dot(vh, rect, RoadA[4], 9f, Vermilion);
            }

            private static void Polyline(VertexHelper vh, Rect rect, Vector2[] points, Color tint, float width)
            {
                for (int i = 1; i < points.Length; i++)
                {
                    Vector2 a = Vector2.Scale(points[i - 1], rect.size) + rect.min;
                    Vector2 b = Vector2.Scale(points[i], rect.size) + rect.min;
                    Vector2 side = (b - a).normalized * (width * 0.5f);
                    AddQuad(vh, a - side, a + side, b + side, b - side, tint);
                }
            }

            private static void Dot(VertexHelper vh, Rect rect, Vector2 point, float size, Color tint)
            {
                Vector2 p = Vector2.Scale(point, rect.size) + rect.min;
                float r = size * 0.5f;
                AddQuad(vh, p + new Vector2(-r, -r), p + new Vector2(-r, r),
                        p + new Vector2(r, r), p + new Vector2(r, -r), tint);
            }

            private static void Quad(VertexHelper vh, Rect rect, Rect uv, Color tint)
            {
                Vector2 a = Vector2.Scale(new Vector2(uv.xMin, uv.yMin), rect.size) + rect.min;
                Vector2 b = Vector2.Scale(new Vector2(uv.xMin, uv.yMax), rect.size) + rect.min;
                Vector2 c = Vector2.Scale(new Vector2(uv.xMax, uv.yMax), rect.size) + rect.min;
                Vector2 d = Vector2.Scale(new Vector2(uv.xMax, uv.yMin), rect.size) + rect.min;
                AddQuad(vh, a, b, c, d, tint);
            }

            private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
            {
                int index = vh.currentVertCount;
                UIVertex v = UIVertex.simpleVert;
                v.color = tint;
                v.position = a; vh.AddVert(v);
                v.position = b; vh.AddVert(v);
                v.position = c; vh.AddVert(v);
                v.position = d; vh.AddVert(v);
                vh.AddTriangle(index, index + 1, index + 2);
                vh.AddTriangle(index, index + 2, index + 3);
            }
        }
    }
}
