# Space.DependencyInjection

Dependency Injection extensions and runtime implementations for the Space framework. Provides the `AddSpace` entry point that wires up all discovered handlers, pipelines, notifications and system modules without runtime reflection.

## Install
```bash
 dotnet add package Space.Abstraction            # brings attributes + source generator analyzer
 dotnet add package Space.DependencyInjection    # brings DI extensions + ISpace implementation
```

## Root Aggregator (Multi-Project)
- Mark exactly one project as root aggregator:
```xml
<PropertyGroup>
  <SpaceGenerateRootAggregator>true</SpaceGenerateRootAggregator>
</PropertyGroup>
```
- That root project MUST reference BOTH `Space.Abstraction` and `Space.DependencyInjection`.
- Satellite libraries that only contain handlers/pipelines/notifications should reference `Space.Abstraction` to bring the analyzer (DI package is optional for them).

## Quick Start
```csharp
var services = new ServiceCollection();
services.AddSpace(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;      // Lifetime of generated registrations
    options.NotificationDispatchType = NotificationDispatchType.Parallel; // or Sequential
});
// Optional built–in module providers
services.AddSpaceInMemoryCache(); // if using InMemoryCache module package

var provider = services.BuildServiceProvider();
var space = provider.GetRequiredService<ISpace>();

// Send a request
var loginResponse = await space.Send<UserLoginResponse>(new UserLoginRequest("demo"));
```

## Source-Generated Extensions (Internal)
The generator (included via Space.Abstraction) emits and `AddSpace` invokes:
- `AddSpaceSourceGenerated(IServiceCollection, Action<SpaceOptions>?)`
- `AddSpaceModules(IServiceCollection)`
You normally just call `services.AddSpace(...)`.

## Notifications
Configure dispatch strategy via `SpaceOptions.NotificationDispatchType` (Parallel or Sequential). Each `[Notification]` method receives a `NotificationContext<TEvent>`.

## Pipelines
Add cross–cutting logic with `[Pipeline]` methods using `PipelineContext<TRequest>`; use `Order` to influence execution relative to other pipelines (modules use very low values to run first).

## Performance
No runtime reflection: all registrations & handler metadata are generated at build time for minimal overhead.

## Links
- Repo: https://github.com/salihcantekin/Space
- License: MIT
