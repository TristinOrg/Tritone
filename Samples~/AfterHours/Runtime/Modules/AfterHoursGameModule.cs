using System;
using System.Threading.Tasks;
using AfterHours.Flows;
using AfterHours.Domain;
using AfterHours.Entities;
using AfterHours.Models;
using AfterHours.UI;
using System.Collections.Generic;
using AfterHours.Generated.Tables;
using AfterHours.Generated.Network;
using Tritone.Content;
using Tritone.Kernel;
using Tritone.Entities;
using Tritone.Tables;
using Tritone.Timing;
using Tritone.Networking;
using Tritone.Unity.Assets;
using Tritone.UI;
using UnityEngine;

namespace AfterHours.Modules
{
    /// <summary>Owns persistent RPG progression and routes deliberate case transitions.</summary>
    public sealed class AfterHoursGameModule : ModuleBase
    {
        public const string ProfileSaveSlot = "after-hours-profile";
        private const string NightWatchTimer = "after-hours-night-watch";
        private AfterHoursEvents mEvents;
        private EncounterRules mEncounterRules;
        private EncounterState mEncounter;
        private Table<string, ClueRow> mClues;
        private Table<string, SkillRow> mSkillRows;
        private readonly Dictionary<int, EntityId> mCombatEntities = new();
        private string mScreen = "office";
        private string mPreviousScreen = "office";

        public int Revision { get; private set; }
        public string Screen => mScreen;
        public AfterHoursProfile Profile => Context.Models.Get<AfterHoursProfile>();
        public CaseSessionModel Session => Context.Models.Get<CaseSessionModel>();
        public EncounterState Encounter => mEncounter;
        public int ClueTableCount => mClues?.Count ?? 0;
        public int SkillTableCount => mSkillRows?.Count ?? 0;
        public bool HasNetworkProfile => Context.Services.TryGet<INetworkService>(out _);
        public bool HasAddressablesProfile => Context.Services.TryGet<IAddressablesCatalogService>(out _);
        public bool HasContentUpdateProfile => Context.Services.TryGet<IContentUpdateService>(out _);
        public string RelayStatus { get; private set; } = "NO NETWORK SERVICE";

        protected override void OnStart()
        {
            mEvents = Context.Services.GetRequired<AfterHoursEvents>();
            mClues = Context.Tables.Load<string, ClueRow>(ClueTable.Path);
            mSkillRows = Context.Tables.Load<string, SkillRow>(SkillTable.Path);
            var skills = new List<EncounterSkill>(mSkillRows.Count);
            for (int i = 0; i < mSkillRows.Count; i++)
            {
                var row = mSkillRows.GetAt(i);
                skills.Add(new EncounterSkill(row.Id, row.FocusCost, row.Power, row.StunTurns));
            }
            mEncounterRules = new EncounterRules(skills);
            LoadProfile();
            Context.UI.AddWindow(typeof(AfterHoursSystemsPanelWindow), "UI/SystemsPanel",
                                 EUILayer.Popup, EUIWindowLifetime.Module);
            Context.Events.Bind(mEvents.CaseResolved, OnCaseResolved);
            if (HasNetworkProfile)
            {
                Context.Network.BindState(OnRelayStateChanged);
                Context.Network.Bind<CaseStatus>(OnRelayCaseStatus);
                RelayStatus = "DISCONNECTED / LOCAL RELAY OPTIONAL";
            }
            else
                RelayStatus = "NO NETWORK SERVICE / OFFLINE PROFILE";
            Context.Input.Bind("Interact", Interact);
            Context.Input.Bind("Inspect", Inspect);
            Context.Input.Bind("OpenManual", ToggleManual);
            Context.Input.Bind("Pause", HandlePause);
            Context.Input.Bind("BasicStrike", () => UseSkill("basic-strike"));
            Context.Input.BindAxis("Horizontal", value => MoveLocation(value > 0f ? 1 : -1), 0.5f);
            Context.Timers.SetRepeated(NightWatchTimer,
                                       8.0d,
                                       () => mEvents.NightTick.Publish(),
                                       ETimerTimeMode.Unscaled);
        }

        protected override void OnStop()
        {
            Context.Timers.Cancel(new TimerKey(NightWatchTimer));
            SaveProfile();
        }

        public async Task BeginCaseAsync()
        {
            await Context.Flows.SwitchAsync<InvestigationFlow>();
            Context.Scenes.SwitchModule<CaseSceneModule>();
            Session.Begin("last-stop", 3);
            SetScreen("investigation");
            mEvents.TranscriptAdded.Publish("CASE OPENED / 01:17 NORTHBOUND");
        }

        public void MoveLocation(int delta)
        {
            if (mScreen != "investigation" || delta == 0)
                return;
            Session.SelectLocation(Session.SelectedLocation + delta);
            Revision++;
        }

        public void Inspect()
        {
            if (mScreen != "investigation")
                return;
            switch (Session.SelectedLocation)
            {
                case 1:
                    RecordEvidence("ticket-stub-117");
                    break;
                case 3:
                    RecordEvidence("platform-zero");
                    break;
                case 0:
                    mEvents.TranscriptAdded.Publish("THE NIGHT CLERK KEEPS ONE HAND ON THE LAST TICKET.");
                    break;
                case 2:
                    mEvents.TranscriptAdded.Publish("THE TRAM HAS NO ROUTE NUMBER ON ITS FRONT DISPLAY.");
                    break;
            }
            Revision++;
        }

        public bool RecordEvidence(string evidenceId)
        {
            var session = Context.Models.Get<CaseSessionModel>();
            if (mClues == null || !mClues.TryGet(evidenceId, out ClueRow clue))
                return false;
            if (!session.RecordEvidence(evidenceId))
                return false;
            mEvents.EvidenceRecorded.Publish(evidenceId);
            mEvents.TranscriptAdded.Publish($"EVIDENCE LOGGED / {clue.Title.ToUpperInvariant()}");
            mEvents.TranscriptAdded.Publish(clue.Detail);
            if (session.ClueCount >= 2)
                mEvents.TranscriptAdded.Publish("TWO SOURCES AGREE / THE STOP IS STILL ACTIVE");
            SaveProfile();
            Revision++;
            return true;
        }

        public bool StartEncounter()
        {
            var session = Context.Models.Get<CaseSessionModel>();
            if (!session.StartEncounter())
                return false;
            mEncounter = new EncounterState(new List<EncounterActor>
            {
                new EncounterActor(1, "Morrow", false, 26, 8, 6, 2, 4),
                new EncounterActor(2, "Rook", false, 22, 9, 5, 1, 3),
                new EncounterActor(90, "Platform Echo", true, 26, 0, 5, 2, 1)
            });
            SetScreen("encounter");
            _ = Context.Flows.SwitchAsync<EncounterFlow>();
            mEvents.TranscriptAdded.Publish("CONTACT / THE PLATFORM ECHO HAS ENTERED THE TRACK BED");
            var world = Context.Entities.Scene;
            mCombatEntities.Clear();
            var existingActors = world.Query<CombatActorComponent>();
            for (int i = 0; i < mEncounter.Actors.Count; i++)
            {
                var actor = mEncounter.Actors[i];
                EntityId entity = default;
                bool found = false;
                for (int j = 0; j < existingActors.Count; j++)
                {
                    ref var existing = ref existingActors.GetComponent(j);
                    if (existing.ActorId != actor.Id)
                        continue;
                    entity = existingActors.GetEntity(j);
                    existing.Health = actor.Health;
                    existing.MaximumHealth = actor.MaxHealth;
                    existing.Focus = actor.Focus;
                    existing.IsEnemy = actor.IsEnemy;
                    found = true;
                    break;
                }
                if (!found)
                {
                    entity = world.Create();
                    world.Add(entity, new CombatActorComponent
                    {
                        ActorId = actor.Id,
                        Health = actor.Health,
                        MaximumHealth = actor.MaxHealth,
                        Focus = actor.Focus,
                        IsEnemy = actor.IsEnemy
                    });
                }
                mCombatEntities[actor.Id] = entity;
            }
            Revision++;
            return true;
        }

        public bool UseSkill(string skillId)
        {
            if (mScreen != "encounter" || mEncounter == null || mEncounter.CurrentActor == null ||
                mEncounter.CurrentActor.IsEnemy)
                return false;

            var actor = mEncounter.CurrentActor;
            var target = FirstLivingEnemy();
            if (target == null || !mEncounterRules.TryUseSkill(mEncounter, actor.Id, target.Id, skillId, out var result))
                return false;
            PublishAction(result);

            while (!mEncounter.IsComplete && mEncounter.CurrentActor != null && mEncounter.CurrentActor.IsEnemy)
            {
                var enemy = mEncounter.CurrentActor;
                var party = FirstLivingParty();
                if (party == null || !mEncounterRules.TryUseSkill(mEncounter, enemy.Id, party.Id, "echo-strike", out result))
                    break;
                PublishAction(result);
            }

            if (mEncounter.IsComplete)
                ResolveCase(mEncounter.PartyWon);
            Revision++;
            return true;
        }

        public void ResolveCase(bool partyWon)
        {
            var session = Context.Models.Get<CaseSessionModel>();
            if (!session.Resolve(partyWon ? CaseOutcome.Resolved : CaseOutcome.Withdrawn))
                return;
            _ = Context.Flows.SwitchAsync<DebriefFlow>();
            mEvents.CaseResolved.Publish(session.Outcome.Value);
            mEvents.ScreenChanged.Publish("debrief");
            Revision++;
        }

        public void ReadResidentInterview()
        {
            mEvents.TranscriptAdded.Publish("CLERK / 'THE PASSENGER PAID WITH A TOKEN FROM A LINE THAT CLOSED IN 1986.'");
            mEvents.TranscriptAdded.Publish("FIELD NOTE / KEEP THE TOKEN. DO NOT PUT IT BACK ON THE TRACK.");
            Revision++;
        }

        public void OpenManual()
        {
            mPreviousScreen = mScreen == "manual" ? mPreviousScreen : mScreen;
            SetScreen("manual");
        }

        public void OpenSettings()
        {
            mPreviousScreen = mScreen == "settings" ? mPreviousScreen : mScreen;
            SetScreen("settings");
        }

        public void OpenSystemsLab()
        {
            mPreviousScreen = mScreen == "systems" ? mPreviousScreen : mScreen;
            if (mScreen == "office")
                SwitchToSystemsLab();
            else
                SetScreen("systems");
        }

        public void OpenDataWorkshop() => SetScreen("data");

        public string GetClueTableRow(int index)
        {
            if (mClues == null || index < 0 || index >= mClues.Count)
                return string.Empty;
            var row = mClues.GetAt(index);
            return $"{row.Id.ToUpperInvariant()}  /  {row.Title}\n{row.Detail}";
        }

        public string GetSkillTableRow(int index)
        {
            if (mSkillRows == null || index < 0 || index >= mSkillRows.Count)
                return string.Empty;
            var row = mSkillRows.GetAt(index);
            return $"{row.Id.ToUpperInvariant()}  /  COST {row.FocusCost:00}  /  POWER {row.Power:00}  /  STUN {row.StunTurns:00}";
        }

        public void OpenParty()
        {
            mPreviousScreen = mScreen == "party" ? mPreviousScreen : mScreen;
            SetScreen("party");
        }

        public void OpenArchive()
        {
            mPreviousScreen = mScreen == "archive" ? mPreviousScreen : mScreen;
            SetScreen("archive");
        }

        public void OpenFrameworkWindow() => Context.UI.Open(typeof(AfterHoursSystemsPanelWindow));

        public void ReturnToOfficeScreen()
        {
            string destination = mPreviousScreen ?? "office";
            if (mScreen == "systems" && destination == "office")
                Context.Scenes.SwitchModule<OfficeSceneModule>();
            SetScreen(destination);
        }

        public void Touch() => Revision++;

        public void SwitchToSystemsLab()
        {
            if (mScreen == "office" && !mScreen.Equals("systems", StringComparison.Ordinal))
                Context.Scenes.SwitchModule<SystemsLabSceneModule>();
            SetScreen("systems");
            mEvents.TranscriptAdded.Publish("SYSTEMS LAB / PROFILE: RESOURCES / RELAY: OFFLINE");
            Revision++;
        }

        public void RunSystemsProbe(string probe)
        {
            switch (probe)
            {
                case "timer":
                    Context.Timers.Set(new TimerKey("after-hours-lab-timer"), 0.7d,
                        () => mEvents.TranscriptAdded.Publish("TIMER / delayed callback arrived on the owning game module."),
                        ETimerTimeMode.Unscaled);
                    mEvents.TranscriptAdded.Publish("TIMER / scheduled one-shot; inspect the transcript in a moment.");
                    break;
                case "tween":
                    Context.Tweens.Play(0f, 1f, 0.9d, value => LabTweenValue = value,
                        Tritone.Tweening.ETweenEase.InOutQuad, ETimerTimeMode.Unscaled,
                        () => mEvents.TranscriptAdded.Publish($"TWEEN / eased to {LabTweenValue:0.00} and completed."));
                    mEvents.TranscriptAdded.Publish("TWEEN / animated probe started with InOutQuad easing.");
                    break;
                case "dispatch-pool":
                    var note = Context.Pools.Rent<ReusableCaseNote>();
                    note.Text = "MAIN THREAD + POOL / callback delivered; rented note returned.";
                    string noteText = note.Text;
                    Context.MainThread.Post(() => mEvents.TranscriptAdded.Publish(noteText));
                    Context.Pools.Return(note);
                    break;
                case "entity":
                    var entity = Context.Entities.Scene.Create();
                    Context.Entities.Scene.Add(entity, new EvidenceMarkerComponent
                    {
                        LocationIndex = 3,
                        HasEvidence = true,
                        Pulse = 0f
                    });
                    mEvents.TranscriptAdded.Publish("ENTITY / scene evidence marker added; ordered signal system now advances its pulse.");
                    break;
                default:
                    return;
            }
            Revision++;
        }

        public async Task ConnectLocalRelayAsync(string host = "127.0.0.1", int port = 27831)
        {
            if (!HasNetworkProfile)
            {
                RelayStatus = "OFFLINE RESOURCES PROFILE / OPEN NetworkDesk SCENE TO ENABLE";
                Revision++;
                return;
            }
            try
            {
                await Context.Network.ConnectAsync(host, port);
                await Context.Network.SendAsync(new CaseStatus
                {
                    CaseId = "last-stop",
                    State = "FIELD_CASE_OPEN"
                });
                RelayStatus = "CONNECTED / GENERATED PROTOCOL ACCEPTED";
            }
            catch (Exception exception)
            {
                RelayStatus = "CONNECT FAILED / " + exception.GetType().Name.ToUpperInvariant();
                Logger.Warning($"The optional relay is unavailable: {exception.Message}");
            }
            Revision++;
        }

        public async Task DisconnectLocalRelayAsync()
        {
            if (!HasNetworkProfile)
                return;
            await Context.Network.DisconnectAsync();
            RelayStatus = "DISCONNECTED / RETRY AVAILABLE";
            Revision++;
        }

        public async Task RunAssetDeliveryProbeAsync()
        {
            try
            {
                if (HasAddressablesProfile)
                {
                    var catalogs = Context.Services.GetRequired<IAddressablesCatalogService>();
                    var catalogResult = await catalogs.UpdateCatalogsAsync();
                    var downloads = Context.Services.GetRequired<IAddressablesDownloadService>();
                    var downloadResult = await downloads.DownloadAsync("after-hours-case-data");
                    mEvents.TranscriptAdded.Publish(
                        $"ADDRESSABLES / {catalogResult.CatalogIds.Count} CATALOG(S) / {downloadResult.DownloadedBytes} BYTES PREFETCHED");
                }
                else if (HasContentUpdateProfile)
                {
                    var result = await Context.Content.UpdateAsync();
                    mEvents.TranscriptAdded.Publish(
                        $"CONTENT UPDATE / {result.Plan.Downloads.Count} BUNDLE(S) / {result.Plan.DownloadBytes} BYTES / {(result.Updated ? "APPLIED" : "CURRENT")}");
                }
                else
                    mEvents.TranscriptAdded.Publish("ASSET DELIVERY / OPEN AN ADDRESSABLES OR CONTENT WORKSHOP PROFILE");
            }
            catch (Exception exception)
            {
                mEvents.TranscriptAdded.Publish("ASSET DELIVERY / " + exception.GetType().Name.ToUpperInvariant());
                Logger.Warning($"The optional asset delivery service is unavailable: {exception.Message}");
            }
            Revision++;
        }

        public async Task RunAssetServiceProbeAsync()
        {
            TextAsset data = null;
            try
            {
                data = await Context.Assets.LoadAsync<TextAsset>("Tables/clues");
                mEvents.TranscriptAdded.Publish(
                    $"ASSET SERVICE / TABLE TEXT LOADED / {data.text.Length} CHARACTERS");
            }
            catch (Exception exception)
            {
                mEvents.TranscriptAdded.Publish("ASSET SERVICE / " + exception.GetType().Name.ToUpperInvariant());
                Logger.Warning($"The selected asset provider could not load case data: {exception.Message}");
            }
            finally
            {
                if (data != null)
                    Context.Assets.Release(data);
            }
            Revision++;
        }

        public float LabTweenValue { get; private set; }

        public void SetLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return;
            Profile.Language = language;
            Context.Settings.Service.SetString("language", language);
            SaveProfile();
            _ = ActivateLanguageAsync(language);
        }

        public string GetLocalizedText(string key) => Context.Localization.Get(key);

        public string GetSelectedLocationNote()
        {
            if (Session.SelectedLocation == 1 && mClues != null && mClues.TryGet("ticket-stub-117", out var ticket))
                return ticket.Title + "\n" + ticket.Detail;
            if (Session.SelectedLocation == 3 && mClues != null && mClues.TryGet("platform-zero", out var platform))
                return platform.Title + "\n" + platform.Detail;
            if (Session.SelectedLocation == 0)
                return "THE NIGHT CLERK KEEPS ONE HAND ON THE LAST TICKET.\n\nA careful interview may connect the route to a person.";
            return "THE TRAM HAS NO ROUTE NUMBER ON ITS FRONT DISPLAY.\n\nInspect the ticket window and platform markers for independent evidence.";
        }

        private async Task ActivateLanguageAsync(string language)
        {
            try
            {
                await Context.Localization.SetLanguageAsync(language);
            }
            catch (Exception exception)
            {
                Logger.Warning($"Could not load language '{language}': {exception.Message}");
            }
            Revision++;
        }

        public void SetSoundVolume(float value)
        {
            var service = Context.Services.GetRequired<Tritone.Audio.IAudioService>();
            service.SoundVolume = Math.Max(0f, Math.Min(1f, value));
            Context.Settings.Service.SetFloat("sound-volume", service.SoundVolume);
            Context.Settings.Service.Save();
            Revision++;
        }

        public float GetSoundVolume() => Context.Services.GetRequired<Tritone.Audio.IAudioService>().SoundVolume;

        public float GetTextScale() => Math.Max(0.85f, Math.Min(1.25f, Context.Settings.Service.GetFloat("text-scale", 1f)));

        public void SetTextScale(float value)
        {
            Context.Settings.Service.SetFloat("text-scale", Math.Max(0.85f, Math.Min(1.25f, value)));
            Context.Settings.Service.Save();
            Revision++;
        }

        public async Task ReturnToOfficeAsync()
        {
            await Context.Flows.SwitchAsync<OfficeFlow>();
            Context.Scenes.SwitchModule<OfficeSceneModule>();
            SetScreen("office");
        }

        public void SaveProfile()
        {
            if (mEvents == null)
                return;
            Context.Saves.Save(ProfileSaveSlot, Context.Models.Get<AfterHoursProfile>());
            Context.Settings.Service.Save();
        }

        private void LoadProfile()
        {
            var profile = Context.Models.Get<AfterHoursProfile>();
            try
            {
                if (Context.Saves.TryLoad(ProfileSaveSlot, out AfterHoursProfile saved))
                    profile.CopyFrom(saved);
            }
            catch (Exception exception)
            {
                Logger.Warning($"Could not load the profile; opening a new case file. {exception.Message}");
                profile.Reset();
            }

            var settings = Context.Settings.Service;
            profile.Language = settings.GetString("language", profile.Language);
            _ = Context.Localization.SetLanguageAsync(profile.Language);
        }

        private void OnCaseResolved(CaseOutcome outcome)
        {
            var profile = Context.Models.Get<AfterHoursProfile>();
            if (outcome == CaseOutcome.Resolved)
            {
                if (!profile.ResolvedCases.Contains("last-stop"))
                    profile.ResolvedCases.Add("last-stop");
                profile.Experience += 40;
                profile.SignalShards += 1;
                if (!profile.ArchiveEntries.Contains("last-stop-platform"))
                    profile.ArchiveEntries.Add("last-stop-platform");
            }
            SaveProfile();
            Revision++;
        }

        private void OnRelayStateChanged(ENetworkState state)
        {
            RelayStatus = state.ToString().ToUpperInvariant();
            Revision++;
        }

        private void OnRelayCaseStatus(CaseStatus message)
        {
            mEvents.TranscriptAdded.Publish($"RELAY / {message.CaseId?.ToUpperInvariant()} / {message.State?.ToUpperInvariant()}");
            Revision++;
        }

        private void Interact()
        {
            if (mScreen == "office")
                _ = BeginCaseAsync();
            else if (mScreen == "investigation")
                Inspect();
            else if (mScreen == "encounter")
                UseSkill("basic-strike");
            else if (mScreen == "manual")
                SetScreen("office");
        }

        private void ToggleManual()
        {
            if (mScreen == "manual")
                SetScreen(mPreviousScreen ?? "office");
            else
                OpenManual();
        }

        private void HandlePause()
        {
            if (mScreen == "encounter")
                ResolveCase(false);
            else if (mScreen == "investigation")
                _ = ReturnToOfficeAsync();
            else if (mScreen != "office")
                ReturnToOfficeScreen();
        }

        private EncounterActor FirstLivingEnemy()
        {
            if (mEncounter == null)
                return null;
            for (int i = 0; i < mEncounter.Actors.Count; i++)
                if (mEncounter.Actors[i].IsEnemy && mEncounter.Actors[i].IsAlive)
                    return mEncounter.Actors[i];
            return null;
        }

        private EncounterActor FirstLivingParty()
        {
            if (mEncounter == null)
                return null;
            for (int i = 0; i < mEncounter.Actors.Count; i++)
                if (!mEncounter.Actors[i].IsEnemy && mEncounter.Actors[i].IsAlive)
                    return mEncounter.Actors[i];
            return null;
        }

        private void PublishAction(EncounterActionResult result)
        {
            var actor = mEncounter.FindActor(result.ActorId);
            var target = mEncounter.FindActor(result.TargetId);
            var world = Context.Entities.Scene;
            if (mCombatEntities.TryGetValue(actor.Id, out var actorEntity) && world.IsAlive(actorEntity))
            {
                ref var actorComponent = ref world.Get<CombatActorComponent>(actorEntity);
                actorComponent.Health = actor.Health;
                actorComponent.Focus = actor.Focus;
            }
            if (mCombatEntities.TryGetValue(target.Id, out var targetEntity) && world.IsAlive(targetEntity))
            {
                ref var targetComponent = ref world.Get<CombatActorComponent>(targetEntity);
                targetComponent.Health = target.Health;
                targetComponent.Focus = target.Focus;
            }
            mEvents.TranscriptAdded.Publish($"{actor.Name.ToUpperInvariant()} / {result.SkillId.ToUpperInvariant()} / {result.Damage:00} IMPACT");
            mEvents.SignalChanged.Publish(Session.SignalReserve);
            mEvents.TurnAdvanced.Publish(mEncounter.Round);
            Revision++;
        }

        private void SetScreen(string screen)
        {
            mScreen = screen;
            mEvents.ScreenChanged.Publish(screen);
            Revision++;
        }
    }
}
