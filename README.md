# Tritone

Tritone is a clean, modular, high-performance game framework for Unity and .NET.

## Current milestone: Kernel

The first milestone provides:

- A pure C# application lifecycle with no Unity dependency.
- Explicit module dependencies with topological startup order.
- Reverse-order shutdown and partial-start rollback.
- A small singleton service registry.
- Allocation-free PreUpdate, Update, LateUpdate, and FixedUpdate dispatch.
- A Unity bootstrap component.
- A filtered, multi-sink diagnostic logging service.

## Unity quick start

Create a module:

```csharp
using Tritone.Kernel;

public sealed class HelloModule : ModuleBase, IUpdateSystem
{
    public int Order => 0;

    protected override ELogLevel LogLevel => ELogLevel.Debug;

    protected override void OnStart()
    {
        Logger.Info("Hello module started.");
    }

    public void Update(in FrameTime time)
    {
    }

    protected override void OnStop()
    {
        Logger.Info("Hello module stopped.");
    }
}
```

Create the project entry point:

```csharp
using Tritone.Diagnostics;
using Tritone.Kernel;
using Tritone.Unity;

public sealed class GameBootstrap : TritoneBootstrap
{
    protected override void Configure(GameApplicationBuilder builder)
    {
        builder.UseLogging(ELogLevel.Debug, new UnityLogSink());
        builder.AddModule(new HelloModule());
    }
}
```

Attach `GameBootstrap` to one GameObject in the startup scene and enter Play Mode.

## Module context and capabilities

Each module receives an independent `ModuleContext`. The context composes
feature capabilities and one generic `ModuleScope`:

```text
ModuleBase
    -> ModuleContext
        -> Assets / Timers / UI / Network / other capabilities
            -> Service contracts
            -> ModuleScope ownership
```

`ModuleBase` keeps familiar helpers such as `LoadAsset`, `SetTimer`, and
`BindMessage` as compatibility facades. Their implementation and ownership now
live in focused capabilities. New framework features should follow the same
boundary: service contracts provide shared behavior, capabilities adapt that
behavior for a module, and the module scope releases acquired resources.

Modules may also use a capability explicitly when that makes a dependency
clearer:

```csharp
var prefab = await Context.Assets.LoadAsync<GameObject>("Characters/Player");
Context.Events.Bind(player.Events.HealthChanged, OnHealthChanged);
```

Unused capabilities are created lazily and allocate no domain-specific scope.

## Model and state quick start

Register shared state with an explicit factory and ownership lifetime:

```csharp
builder.AddApplicationModel<PlayerModel>();
builder.AddSceneModel(() => new BattleModel(mBattleRules));
```

Models implement a deterministic lifecycle without depending on Unity:

```csharp
public sealed class PlayerModel : IModel
{
    public readonly Event<int> LevelChanged = new();

    public int Level { get; private set; }

    public void Initialize()
    {
        Level = 1;
    }

    public void Reset()
    {
        Level = 1;
        LevelChanged.Clear();
    }

    public void Dispose()
    {
        LevelChanged.Clear();
    }
}
```

Resolve a model directly from a module capability or use the compatibility
facade:

```csharp
var player = Context.Models.Get<PlayerModel>();
var battle = GetModel<BattleModel>();
```

Models are created only on first access and shared by concrete registered type.
Application models survive scene changes and release in reverse creation order
when the application stops. Scene models require an active scene module and are
released before the next scene module starts. Consumers never own shared model
instances, so stopping one module cannot invalidate state still used elsewhere.
Initialization failures dispose the incomplete instance and leave registration
available for a later retry.

## Game flow quick start

Register application flows explicitly:

```csharp
builder.AddFlow(() => new LoginFlow(mLoginConfig));
builder.AddFlow<LobbyFlow>();
builder.AddFlow<BattleFlow>();
```

A flow owns one high-level application stage:

```csharp
public sealed class LoginFlow : IFlow
{
    public async Task EnterAsync(CancellationToken cancellationToken)
    {
        await LoadLoginDataAsync(cancellationToken);
    }

    public void Update(in FrameTime time)
    {
    }

    public void Exit()
    {
    }

    public void Dispose()
    {
    }
}
```

Switch from a module through its capability or compatibility facade:

```csharp
await Context.Flows.SwitchAsync<LoginFlow>(cancellationToken);
await SwitchFlowAsync<BattleFlow>(cancellationToken);
```

Only one flow is active at a time. Identical concurrent requests share the
running transition, while conflicting targets are rejected. The current flow
exits before the target enters, but is retained until entry succeeds. Failed or
cancelled entry disposes the incomplete target and re-enters the previous flow.
The active flow updates before normal module updates and exits automatically
when the application stops.

## Entity world quick start

Register struct component data and ordered systems explicitly for the world
lifetime that owns them:

```csharp
builder.UseEntities(initialCapacity: 256);
builder.AddApplicationComponent<PlayerIdentity>();
builder.AddSceneComponent<Position>();
builder.AddSceneComponent<Velocity>();
builder.AddSceneEntitySystem<MovementSystem>();
```

Component data remains plain value types:

```csharp
public struct Position : IEntityComponent
{
    public float X;
    public float Y;
}
```

Create entities and mutate components by reference:

```csharp
EntityWorld world = Context.Entities.Scene;
EntityId entity = world.Create();
world.Add(entity, new Position { X = 10.0f });

ref Position position = ref world.Get<Position>(entity);
position.X += 5.0f;
```

Queries are lightweight structs. Indexed traversal does not allocate an
enumerator and exposes component references directly:

```csharp
var query = world.Query<Position, Velocity>();
for (int i = 0, cnt = query.CandidateCount; i < cnt; i++)
{
    if (!query.TryGetEntity(i, out var entity))
        continue;

    ref Position position = ref query.GetFirst(entity);
    ref Velocity velocity = ref query.GetSecond(entity);
    position.X += velocity.X;
}
```

`EntityId` contains a slot index and generation, so a destroyed identifier
cannot access a later entity that reuses the same slot. Application worlds
survive scene changes. Scene worlds are created before their scene module
configures and release all systems, entities, and components after that module
stops. Entity systems initialize in stable order, update before normal modules,
and shut down in reverse order.

## Timer quick start

Register the shared timer scheduler once:

```csharp
builder.UseTimers();
builder.AddModule(new GameplayModule());
```

Use integer or string keys directly from any `ModuleBase` implementation:

```csharp
protected override void OnStart()
{
    SetTimer("Refresh", 2.0, OnRefresh);
    SetRepeatedTimer(1001, 1.0, OnSecondPassed);
}

private void OnSecondPassed()
{
    Logger.Debug("One second passed.");
}

private void OnRefresh()
{
    CancelTimer(1001);
}
```

Each module can own multiple keyed timers. Setting the same key replaces its previous timer, and stopping the module automatically cancels every timer it owns and clears its key cache.

Use cached method-group delegates on frequently scheduled paths. Capturing lambdas can allocate even though the timer scheduler itself reuses preallocated storage.

## Event quick start

Keep strongly typed events together in the module that owns them:

```csharp
public sealed class PlayerModule : ModuleBase
{
    public EventsList EventsList { get; } = new();

    public sealed class EventsList
    {
        public readonly Event<int> HealthChanged = new();
        public readonly Event<int, PlayerData> PlayerDied = new();
    }

    private void NotifyHealthChanged(int health)
    {
        EventsList.HealthChanged.Publish(health);
    }
}
```

Keep prefab references in a view without adding business logic:

```csharp
public sealed class UIShopView : UIView
{
    public Button        BtnClose;
    public Button        BtnRule;
    public RectTransform NodePanels;
}
```

Bind Unity controls and Tritone events in the window's dedicated binding stage:

```csharp
public sealed class ShopWindow : UIWindow<UIShopView>
{
    protected override void OnBindEvents()
    {
        BindButton(mView.BtnClose, Close);
        BindButton(mView.BtnRule, OnRule);

        var playerModule = GetModule<PlayerModule>();
        BindEvent(playerModule.EventsList.HealthChanged, OnHealthChanged);
    }

    private void OnHealthChanged(int health)
    {
    }

    private void OnRule()
    {
    }
}
```

`UIElement<TView>` resolves the view once, binds listeners during `OnBindEvents`, and releases every Unity and Tritone listener when disabled. `ModuleBase` releases all of its bindings when the module stops.

Enable assets and UI once in the bootstrap:

```csharp
protected override void Configure(GameApplicationBuilder builder)
{
    builder.UseAssets();
    builder.UseUI(mUIRoot);
    builder.AddModule(new ShopModule());
}
```

Each module registers the windows it owns without modifying a central catalog:

```csharp
public sealed class ShopModule : ModuleBase
{
    protected override void OnConfigure(IServiceRegistry services)
    {
        AddWindow<ShopWindow>("UI/Shop/ShopWindow", EUILayer.Normal);
    }

    private void ShowShop()
    {
        OpenWindow<ShopWindow>();
    }

    private async Task ShowShopAsync()
    {
        await OpenWindowAsync<ShopWindow>();
    }
}
```

Module windows are available while at least one active module owns the matching definition. Repeated registrations must use the same path, layer, and lifetime. Releasing the final owner closes the window, destroys its cached instance, releases its prefab, and removes the definition so a later hot-update module can register a new path. Application windows remain available until the application stops.

## Scene module switching

Persistent infrastructure modules start with the application. Scene modules are registered as factories and created only when entered:

```csharp
builder.UseTimers();
builder.UseAssets();
builder.UseUI(mUIRoot);
builder.UseScenes();
builder.AddSceneModule<LoginModule>();
builder.AddSceneModule(() => new BattleModule(mBattleConfig));
```

Load the target Unity scene and activate its module from a `ModuleBase` or `TritoneComponent`:

```csharp
await SwitchSceneAsync<LoginModule>("Login", OnSceneProgress);
await SwitchSceneAsync<BattleModule>("Battle");
```

The target scene loads additively before the previous module stops. Tritone then makes it active, creates its fresh scene module, and unloads the previous scene. Identical concurrent requests share one operation, while conflicting targets are rejected. A loading failure leaves the previous scene and module untouched; a module startup failure restores the previous scene and recreates its module.

`TritoneBootstrap` automatically survives scene unloading. Scene module timers, event bindings, windows, pools, assets, and tables are released when that module stops. Data that must survive transitions should live in a persistent model module. Use `SwitchModule<TModule>()` only when changing logical modules without loading a Unity scene.

## Pool quick start

Enable shared lazy pools once without registering object types or prefabs:

```csharp
builder.UsePools();
```

Rent and return plain C# objects directly from a `ModuleBase`:

```csharp
var damageData = Rent<DamageData>();
Return(ref damageData);
```

Spawn and despawn Unity Component or GameObject prefabs without prior pool registration:

```csharp
var effect = Spawn(mDamageEffectPrefab, mEffectRoot);
Despawn(ref effect);
```

The `ref` overload clears the caller's reference after a successful return. The first request creates the matching type or prefab pool. Objects left active are automatically returned when their owning module or `TritoneComponent` is released. `UIElement` also returns everything spawned during its current enabled lifetime when it closes, so temporary UI children do not require manual despawn calls. Implement `IPoolable` only when an object needs spawn and despawn reset callbacks.

## Basic experience services

Configure the four optional services once:

```csharp
builder.UseAssets();
builder.UseAudio();
builder.UseSaves();
builder.UseSettings();
builder.UseTables();
builder.UseLocalization("en");
```

Use audio directly from a module or Tritone component:

```csharp
PlayMusic("Audio/Music/Login");
var click = PlaySound("Audio/SFX/Click");
StopSound(click);
```

Save strongly typed data atomically:

```csharp
Save("slot1", playerSave);

if (TryLoadSave<PlayerSave>("slot1", out var loaded))
    ApplySave(loaded);
```

Settings remain in memory until `Settings.Save()` or application shutdown:

```csharp
Settings.SetFloat("MusicVolume", 0.8f);
Settings.SetBool("Muted", false);
Settings.SetString("Language", "zh-CN");
```

Localization reads one hot-updateable JSON table per language from
`Localization/{language}`:

```csharp
var title = Localize("UI.Login.Title");
await SetLanguageAsync("zh-CN");
```

Each localization file uses the same table JSON shape:

```json
{
  "Rows": [
    { "Id": "UI.Login.Title", "Text": "Login" }
  ]
}
```

## Asset quick start

Enable asset management with the built-in Unity Resources provider:

```csharp
builder.UseAssets();
```

Load assets directly from a `ModuleBase` or `TritoneComponent`:

```csharp
var config = LoadAsset<TextAsset>("Configs/GameConfig");
var prefab = await LoadAssetAsync<GameObject>("UI/LoginWindow");
```

Repeated path and type requests share one cached load. Concurrent asynchronous calls also join the same provider operation. Manual release is optional:

```csharp
ReleaseAsset(config);
```

Every remaining reference is released automatically when its owning module stops or its `TritoneComponent` is destroyed. Implement `IAssetProvider` and pass it to `UseAssets(provider)` when replacing Resources with Addressables or another backend.

## Configuration table quick start

Create `Assets/Tritone/Tables.json` and run `Tritone/Generate/Tables` to generate
strongly typed rows without manually maintaining boilerplate:

```json
{
  "Namespace": "Game.Tables",
  "OutputPath": "Assets/Generated/Tritone/Tables",
  "Tables": [
    {
      "Name": "Role",
      "Path": "Tables/Roles",
      "Fields": [
        { "Name": "Id", "Type": "int", "Key": true },
        { "Name": "Name", "Type": "string", "Key": false }
      ]
    }
  ]
}
```

Generated files are rewritten only when their contents change. Removed table
definitions also remove their former generated source files.

Enable tables after configuring either Resources or content-managed assets:

```csharp
builder.UseAssets();
builder.UseTables();
```

Describe a row with its stable primary key:

```csharp
[Serializable]
public sealed class RoleRow : ITableRow<int>
{
    public int    Id;
    public string Name;

    public int Key => Id;
}
```

Store the rows in a UTF-8 JSON `TextAsset`:

```json
{
  "Rows": [
    { "Id": 1001, "Name": "Tristin" },
    { "Id": 1002, "Name": "Aigis" }
  ]
}
```

Load and query the table directly from any `ModuleBase`:

```csharp
var roles  = LoadTable<int, RoleRow>("Tables/Roles");
var tristin = roles.Get(1001);

if (roles.TryGet(1002, out var aigis))
    Logger.Info(aigis.Name);
```

Asynchronous loading uses the same ownership model:

```csharp
var roles = await LoadTableAsync<int, RoleRow>("Tables/Roles");
```

The first load deserializes and indexes the rows once. Repeated loads share the parsed table, and concurrent asynchronous requests join one operation. Primary-key lookup is constant time, while `GetAt(index)` supports allocation-free source-order traversal. Duplicate keys fail immediately during indexing.

Manual release is optional. `ReleaseTable(ref roles)` releases one owned reference and clears the caller. Any remaining tables and their underlying `TextAsset` references are released automatically when the owning module stops. Pass a custom `ITableDeserializer` to `UseTables(deserializer)` when switching from readable JSON to a generated binary format; gameplay loading and lookup code stays unchanged.

## Network message generation

Create `Assets/Tritone/Network.json` and run
`Tritone/Generate/Network Messages`:

```json
{
  "Namespace": "Game.Network",
  "OutputPath": "Assets/Generated/Tritone/Network",
  "Messages": [
    {
      "Id": 1001,
      "Name": "LoginRequest",
      "Kind": "Request",
      "Response": "LoginResponse",
      "Fields": [
        { "Name": "Account", "Type": "string" }
      ]
    },
    {
      "Id": 1002,
      "Name": "LoginResponse",
      "Kind": "Response",
      "Fields": [
        { "Name": "Token", "Type": "string" }
      ]
    }
  ]
}
```

Register every generated codec once:

```csharp
MessageSerializer serializer = new();
NetworkMessages.Register(serializer);
```

Generated request relationships let modules infer the response type:

```csharp
LoginResponse response = await RequestAsync(
    new LoginRequest { Account = "Tristin" });
```

Message IDs, duplicate names, request-response relationships, and supported
field types are validated before output. Supported network field types are
`bool`, `byte[]`, `float`, `int`, and `string`.

Run `Tritone/Generate/All` when both schemas are present to update tables and
network messages with a single Unity asset refresh.

## Local AssetBundle provider

Compose bundle and asset registrations before building the application:

```csharp
AssetBundleRegistry registry = new();
registry.AddBundle("core", "core.bundle")
        .AddBundle("shared", "shared.bundle", "core")
        .AddBundle("ui", "ui.bundle", "shared")
        .AddAsset("UI/LoginWindow", "ui", "Assets/Game/UI/LoginWindow.prefab");

var bundleRoot = Path.Combine(Application.persistentDataPath, "Bundles");
var source     = new FileAssetBundleSource(bundleRoot);
var provider   = new AssetBundleAssetProvider(registry, source);
builder.UseAssets(provider);
```

Game modules continue using the same API:

```csharp
var prefab = await LoadAssetAsync<GameObject>("UI/LoginWindow");
```

The registry validates missing dependencies and cycles once, then precomputes a unique dependency-first load order. Assets share loaded bundles and in-flight bundle requests. Releasing the final asset unloads its root bundle and dependencies in reverse order. The registry becomes immutable after provider construction; feature code may compose registrations before that point without editing a central ScriptableObject. `FileAssetBundleSource` is the local first-stage source. A later remote source can implement `IAssetBundleSource` without changing AssetModule or gameplay calls.

## Content update planning

Describe one installed or remote content version with immutable bundle and asset entries:

```csharp
var remoteManifest = new ContentManifest(
    "1.1.0",
    new[]
    {
        new ContentBundle("core", "core.bundle", coreHash, coreSize),
        new ContentBundle("ui", "ui.bundle", uiHash, uiSize, "core")
    },
    new[]
    {
        new ContentAsset("UI/LoginWindow", "ui", "Assets/Game/UI/LoginWindow.prefab")
    });
```

Compare the installed manifest with the remote manifest before downloading files:

```csharp
var plan = ContentUpdatePlanner.CreatePlan(localManifest, remoteManifest);

for (int i = 0, cnt = plan.Downloads.Count; i < cnt; i++)
{
    var bundle = plan.Downloads[i];
    // The transactional updater downloads and verifies this file.
}
```

Planning compares bundle file names, hashes, and sizes instead of trusting the version label alone. It reuses identical local files after logical bundle renames, reports obsolete files separately, and preserves remote manifest order for deterministic downloads. After a successful update, create the loading registry directly from the active manifest:

```csharp
var registry = remoteManifest.CreateAssetBundleRegistry();
var provider = new AssetBundleAssetProvider(registry, source);
builder.UseAssets(provider);
```

## Transactional content updates

Configure remote updates and local AssetBundle loading together. Do not also call `UseAssets`:

```csharp
var localRoot = Path.Combine(Application.persistentDataPath, "Content");

ContentUpdateOptions options = new(
    "https://cdn.example.com/Windows/content-manifest.json",
    "https://cdn.example.com/Windows/",
    localRoot);

builder.UseContentAssets(options);
builder.AddModule(new StartupModule());
```

Start the update from a normal module without writing `async void` or managing cancellation:

```csharp
public sealed class StartupModule : ModuleBase
{
    protected override void OnStart()
    {
        StartContentUpdate(OnContentReady,
                           OnContentProgress,
                           OnContentFailed);
    }

    private void OnContentReady(ContentUpdateResult result)
    {
        SwitchModule<LoginModule>();
    }

    private void OnContentProgress(ContentUpdateProgress progress)
    {
        Logger.Info(
            $"Content: {progress.NormalizedProgress:P0}");
    }

    private void OnContentFailed(Exception exception)
    {
        Logger.Error("Content update failed.", exception);
    }
}
```

Use `await UpdateContentAsync(OnContentProgress)` instead when the caller already owns an asynchronous startup flow. Stopping the module automatically cancels its request. The shared updater serializes concurrent checks, streams downloads directly to temporary files, verifies exact size and lowercase SHA-256, and only then enters its non-cancellable commit stage.

The transaction keeps backups and creation markers beneath `.tritone-update`. A failed verification leaves active files untouched. A failed commit restores every replaced or removed file and the previous manifest. If the process terminates during commit, the next update check recovers the unfinished transaction before contacting the server. An update gate rejects the operation while old assets are loaded or loading, blocks new loads during the transaction, and activates the new manifest only after every bundle operation succeeds.

The installed manifest is loaded during application construction, so an existing verified version remains available when the remote check fails. Run content updates before loading content assets; changing files on disk cannot replace AssetBundles or prefab instances that are already in memory.
