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

Unity UI can bind without manually unbinding each event:

```csharp
public sealed class PlayerPanel : TritoneBehaviour
{
    protected override void OnBindEvents()
    {
        var playerModule = GetModule<PlayerModule>();
        BindEvent(playerModule.EventsList.HealthChanged, OnHealthChanged);
    }

    private void OnHealthChanged(int health)
    {
    }
}
```

`TritoneBehaviour` releases all bindings on disable and binds them again on enable. `ModuleBase` releases all of its bindings when the module stops.
