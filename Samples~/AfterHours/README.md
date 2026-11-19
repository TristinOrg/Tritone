# Tritone Feature Playground

### Small, independent Unity demos for Tritone

![After Hours — a night route through the city](Art/after-hours-banner.svg)

**Choose one feature. Try one action. See one result.**

This sample is a set of separate, offline-first scenes for exploring Tritone. Each local demo focuses on a single framework capability and can be understood without playing a game. The Sample Hub is the launcher; every scene has a clear action and a way back.

The visual language uses ink-dark navy, warm paper, transit-map geometry, and restrained signal teal. The banner is an authored SVG included with the sample.

**Languages:** English · [简体中文](README.zh-CN.md) · [日本語](README.ja.md)

---

## Start in Unity

1. Install Tritone in Unity 2022.3 LTS.
2. Import **Tritone Feature Playground** from the package's **Samples** tab.
3. Choose **Tritone → After Hours → Open Sample Hub**, then press **Play**. The command prepares the sample under `Assets/AfterHours` and registers its scenes in Build Settings.
4. Pick a feature, follow its on-screen guide, and use **Back to Sample Hub** to try another.

To open one demo directly, use its scene from `Assets/AfterHours/Scenes`. Each scene shows what to try and what changed. The provider and network demos list their local setup in the scene.

## Choose a scene

| Scene | Feature | What to try |
| --- | --- | --- |
| `ApplicationLab` | Application lifecycle | Inspect the running application and click twice to see the module counter advance. |
| `FlowLab` | Flow transitions | Click twice to enter Running and then Ready. The old flow exits when the new flow takes over. |
| `ModelLab` | Shared models | Increment the shared model twice, reset it, then increment again to see the count restart. |
| `ScopeLab` | Module scopes | Publish once, then click again to dispose the binding and publish without a listener. Reset reattaches it. |
| `DiagnosticsLab` | Runtime diagnostics | Write a module log and see the buffer count and sampled frame index. |
| `DispatcherLab` | Main-thread dispatch | Queue a callback and watch Unity's next update execute it and display its thread ID. |
| `EventLab` | Typed events | Publish several times and watch the listener receive each numbered signal. |
| `TweenLab` | Tween animation | Play the tween and watch the bar fill from 0% to 100%. Reset during playback to cancel. |
| `TimerLab` | Timers | Schedule and cancel a one-shot callback. |
| `SceneModuleLab` | Scene modules | Switch between Lobby and Round without reloading the current Unity scene. |
| `InputLab` | Named input | Click to send the Interact action and see the counter increase. The E key also works when legacy keyboard input is enabled. |
| `AudioLab` | Sound playback | Unmute the Game view, play the chime, and use Reset to stop it. |
| `SaveLab` | Save and load | Save and reload the counter. Leave and re-enter to continue, or Reset to clear this sample slot. |
| `SettingsLab` | Typed settings | Cycle the stored sample level from 0 to 3. The result reads back the value after each change. |
| `LocalizationLab` | Localization | Switch language and watch the same office.open key resolve to different text. |
| `EcsLab` | ECS — spawn and query entities | Click Spawn Marker to add an entity. Watch the live query count and reset the World. |
| `UiLab` | UI — open and close a Tritone window | Open one generated window on the Popup layer, close it, and reopen it. |
| `PoolLab` | Object pooling | Click three times to rent, return, and rent again. Watch the active stage and check whether Tritone reused the same instance. |
| `DataWorkshop` | Tables — load one typed data table | Load the generated ClueRow table and inspect its row count and first entry. |
| `ResourcesLab` | Resources loading | Load the clue text through a Tritone scope and read its character count. The scope releases its reference. |
| `ContentWorkshop` | Content updates & transactions | Inspect the offline content update, staging, commit, and rollback workflow. |
| `AddressablesDesk` | Addressables provider profile | Load the same sample asset through the optional Addressables-backed profile. |
| `BundleDesk` | AssetBundle provider profile | Load content from a local bundle with its manifest and provider setup. |
| `NetworkDesk` | Network — send a local sample request | Open the isolated network sample. It lists the optional local relay setup. |
| `ProtocolLab` | Handshake and reconnect | Start LocalServices/relay.js with Node.js, then connect. Stop and restart it to see reconnection states. |

## Feature coverage

- **Application and modules:** bootstrap composition, module ordering, application/scene lifetime, contexts, scopes, scene modules and scene transitions.
- **Runtime services:** diagnostics and structured logs, main-thread dispatch, typed events, timers, tweens, object pooling, input, audio, models, saves, settings and localization.
- **Entities:** entity worlds, component registration, queries and ordered systems.
- **UI:** `UIWindow`, generated `UIView` references, prefab binding, popup sorting and window lifetime.
- **Tables:** schema-driven typed rows, CSV/JSON input, generation, runtime loading and sample validation.
- **Content:** Resources, Addressables and AssetBundle providers; content manifests, update planning, integrity checks and transactional recovery.
- **Networking:** generated messages, TCP sessions, protocol compatibility, heartbeat and reconnect behavior.
- **Editor workflows:** provider setup, scene authoring, UI binding, table/network generation and content builds.

Editor-only commands are listed in the relevant sample and use the `Tritone/After Hours/…` menu. They are not presented as runtime buttons.

## Optional local profiles

The Addressables, AssetBundle, content-update and Network samples are separate bootstraps. Run only the profile you want to inspect; they do not get combined with the local feature demos.

- **Content update:** run **Tritone → After Hours → Prepare Provider Labs**, then **Build Local Bundle and Update Manifest**. Start the included loopback content host with `node "<imported sample folder>/LocalServices/content-host.js"`.
- **Addressables:** install/enable Unity Addressables for the host project, then run **Prepare Provider Labs** and **Build Addressables Lab Content**.
- **AssetBundle:** run **Prepare Provider Labs**, then **Build Local Bundle and Update Manifest**.
- **Network:** start `node "<imported sample folder>/LocalServices/relay.js"` from a terminal, then use **Network Desk**. The relay binds to `127.0.0.1` only.

If an optional dependency is unavailable, the Hub and offline demos still work.

## Authoring commands

After importing the sample, use **Tritone → After Hours** in Unity:

- **Generate Sample Scenes** — create Hub and focused demo scenes; add them to Build Settings.
- **Regenerate Table Outputs** — compile `Schemas/Tables.json` and CSV into typed rows and JSON resources. The sample `ITableValidator` checks the authored clue data.
- **Regenerate Network Messages** — generate message types, codecs, registry and schema fingerprint from `Schemas/Network.json`.
- **Generate Systems Window View** / **Bind Generated Systems Window View** — generate and bind the strongly typed sample UI view.
- **Prepare Provider Labs** — set up provider-specific sample content.
- **Build Addressables Lab Content** / **Build Local Bundle and Update Manifest** — build the selected optional asset profile.
- **Tritone → Build → Content for Active Platform** — run the shared content build for the selected Unity target.

## Package layout

```text
AfterHours/
├── Art/                 authored SVG banner
├── Data/                sample CSV sources
├── Editor/              scene, table, UI, network and content authoring
├── LocalServices/       optional loopback relay and content host
├── Runtime/             isolated demo bootstraps, UI, data and generated code
├── Schemas/             table and network schemas
├── Scenes/              Hub and individual feature/profile scenes
└── Tests/EditMode/      sample data, catalog and rule tests
```

## Validation

From the Tritone repository root, validate the package with:

```powershell
.\.ci\Validate-Package.ps1
```

Run the `AfterHours.Sample.Tests` EditMode assembly in Unity Test Runner to verify catalog routes, sample data and demo rules.

---

**[中文文档](README.zh-CN.md)** · **[日本語ドキュメント](README.ja.md)**

