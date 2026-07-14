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
