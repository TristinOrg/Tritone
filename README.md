# Tritone

Tritone is a clean, modular, high-performance game framework for Unity and .NET.

## Current milestone: Kernel

The first milestone provides:

- A pure C# application lifecycle with no Unity dependency.
- Explicit module dependencies with topological startup order.
- Reverse-order shutdown and partial-start rollback.
- A small singleton service registry.
- Allocation-free per-frame update dispatch.
- A Unity bootstrap component.

## Unity quick start

Create a module:

```csharp
using Tritone.Kernel;
using UnityEngine;

public sealed class HelloModule : IModule, IUpdateSystem
{
    public int Order => 0;

    public void Configure(IServiceRegistry services)
    {
    }

    public void Start()
    {
        Debug.Log("Tritone started.");
    }

    public void Update(in FrameTime time)
    {
    }

    public void Stop()
    {
        Debug.Log("Tritone stopped.");
    }
}
```

Create the project entry point:

```csharp
using Tritone.Kernel;
using Tritone.Unity;

public sealed class GameBootstrap : TritoneBootstrap
{
    protected override void Configure(GameApplicationBuilder builder)
    {
        builder.AddModule(new HelloModule());
    }
}
```

Attach `GameBootstrap` to one GameObject in the startup scene and enter Play Mode.
