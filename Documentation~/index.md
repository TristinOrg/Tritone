# Tritone Framework

Tritone is a modular Unity framework whose Kernel remains independent of `UnityEngine`. Applications compose only the services they need through `GameApplicationBuilder`, and every module owns a deterministic scope that releases resources during shutdown.

## Requirements

- Unity 2022.3 or newer
- Addressables 1.21.21 or a compatible newer release

## Installation

In Unity Package Manager, choose **Add package from git URL** and enter:

```text
https://github.com/TristinOrg/Tritone.git
```

For a reproducible production project, append a release tag:

```text
https://github.com/TristinOrg/Tritone.git#&lt;release-tag&gt;
```

## Minimal setup

Create a `TritoneBootstrap` subclass and register the required modules:

```csharp
using Tritone.Kernel;
using Tritone.Unity;

public sealed class GameBootstrap : TritoneBootstrap
{
    protected override void Configure(GameApplicationBuilder builder)
    {
        builder.UseMainThreadDispatcher();
        builder.UseAddressableAssets();
        builder.UseTimers();
        builder.AddModule(new GameModule());
    }
}
```

Attach the bootstrap to one object in the startup scene. It starts before normal behaviours, survives scene changes, forwards Unity update stages, and disposes the application when destroyed.

## Validation

The package contains EditMode tests and package-level PlayMode smoke tests. The repository CI also verifies manifest metadata, unique Unity GUIDs, matching `.meta` files, and the absence of `UnityEditor` references from runtime source.

See the repository [README](https://github.com/TristinOrg/Tritone#readme) for feature-specific examples and architecture details.
