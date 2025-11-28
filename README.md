# Space

High-performance, source-generator powered mediator / messaging framework for .NET. Eliminates runtime reflection, minimizes boilerplate, and provides an extensible module + pipeline model for cross-cutting concerns (e.g., caching, auditing).

---
## Status & Packages

| CI | Branch | Status |
|----|--------|--------|
| Prod (Stable Publish) | `master` | ![Prod CI](https://github.com/salihcantekin/Space/actions/workflows/prod-ci.yml/badge.svg) |
| Dev (Preview Publish) | `dev` | ![Dev CI](https://github.com/salihcantekin/Space/actions/workflows/dev-ci.yml/badge.svg) |
| Validation (PR / Feature) | PRs -> `dev` / `master` | ![Validation Build](https://github.com/salihcantekin/Space/actions/workflows/validation-build.yml/badge.svg) |

### NuGet Packages

| Package | Stable | Preview | Downloads | Description |
|---------|--------|---------|-----------|-------------|
| Space.Abstraction | ![NuGet](https://img.shields.io/nuget/v/Space.Abstraction.svg) | ![NuGet (pre)](https://img.shields.io/nuget/vpre/Space.Abstraction.svg) | ![Downloads](https://img.shields.io/nuget/dt/Space.Abstraction.svg) | Core abstractions + Source Generator analyzer (attributes, contexts, contracts) |
| Space.DependencyInjection | ![NuGet](https://img.shields.io/nuget/v/Space.DependencyInjection.svg) | ![NuGet (pre)](https://img.shields.io/nuget/vpre/Space.DependencyInjection.svg) | ![Downloads](https://img.shields.io/nuget/dt/Space.DependencyInjection.svg) | DI extensions & runtime implementations (ISpace) |
| [Space.Modules.InMemoryCache](https://github.com/salihcantekin/Space.Modules.InMemoryCache) | ![NuGet](https://img.shields.io/nuget/v/Space.Modules.InMemoryCache.svg) | ![NuGet (pre)](https://img.shields.io/nuget/vpre/Space.Modules.InMemoryCache.svg) | ![Downloads](https://img.shields.io/nuget/dt/Space.Modules.InMemoryCache.svg) | In-memory caching module + attribute integration |

### Breaking Change (Packaging)
Old behavior: `Space.DependencyInjection` implicitly brought abstractions + source generator.
New behavior: Source generator analyzer ships with `Space.Abstraction`. You must reference BOTH packages for full DI usage.

Migration:
1. Add `Space.Abstraction` to all projects that use Space attributes.
2. Add `Space.DependencyInjection` only where you need `ISpace` and registration extensions.
3. Remove any direct `Space.SourceGenerator` references; they are now unnecessary.

### Install (Minimal DI Usage)
```bash
dotnet add package Space.Abstraction

dotnet add package Space.DependencyInjection
```
Optional module:
```bash
dotnet add package Space.Modules.InMemoryCache
```
If you only need compile-time generation (e.g., custom runtime) reference just `Space.Abstraction`.

---
## Quick Start
```csharp
using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;

var services = new ServiceCollection();
services.AddSpace(opt =>
{
    opt.NotificationDispatchType = NotificationDispatchType.Parallel; // or Sequential
});

services.AddSpaceInMemoryCache(); // if caching module needed

var provider = services.BuildServiceProvider();
var space = provider.GetRequiredService<ISpace>();

public sealed record UserLoginRequest(string UserName) : IRequest<UserLoginResponse>;
public sealed record UserLoginResponse(bool Success);

public class UserHandlers
{
    [Handle]
    public ValueTask<UserLoginResponse> Login(HandlerContext<UserLoginRequest> ctx)
        => ValueTask.FromResult(new UserLoginResponse(true));
}

// Interface-based (optional, for method signature hint only - [Handle] attribute is still required)
public class UserHandlersInterface : IHandler<UserLoginRequest, UserLoginResponse>
{
    [Handle]
    public ValueTask<UserLoginResponse> Handle(HandlerContext<UserLoginRequest> ctx)
        => ValueTask.FromResult(new UserLoginResponse(true));
}

var response1 = await space.Send<UserLoginResponse>(new UserLoginRequest("demo"));
var response2 = await space.Send<UserLoginRequest, UserLoginResponse>(new UserLoginRequest("demo"));
```

### Named Handlers
```csharp
public class PricingHandlers
{
    [Handle(Name = "Default")] public ValueTask<PriceResult> GetDefault(HandlerContext<PriceQuery> ctx) => ...;
    [Handle(Name = "Discounted")] public ValueTask<PriceResult> GetDiscounted(HandlerContext<PriceQuery> ctx) => ...;
}
var discounted = await space.Send<PriceResult>(new PriceQuery(...), name: "Discounted");
```

### Pipelines
```csharp
public class LoggingPipeline
{
    [Pipeline(Order = 100)]
    public async ValueTask<UserLoginResponse> Log(PipelineContext<UserLoginRequest> ctx, PipelineDelegate<UserLoginRequest, UserLoginResponse> next)
    {
        var result = await next(ctx);
        return result;
    }
}
```

### Notifications
```csharp
public sealed record UserLoggedIn(string UserName);
public class LoginNotifications
{
    [Notification]
    public ValueTask Log(NotificationContext<UserLoggedIn> ctx) => ValueTask.CompletedTask;
}
await space.Publish(new UserLoggedIn("demo"));
```

### Caching Module Example
```csharp
public class UserQueries
{
    [Handle]
    [CacheModule(Duration = 60)]
    public ValueTask<UserProfile?> GetUser(HandlerContext<UserId> ctx) => ...;
}
```
`[CacheModule]` (from Space.Modules.InMemoryCache) inserts caching logic before user pipelines.

---
## Multi-Project Setup

Space supports handlers, pipelines, notifications, and modules across multiple projects through a **root aggregator** model. This enables modular solutions where feature libraries can contain their own handlers without manual DI wiring.

### Configuration

Set exactly **one** project as the root aggregator (typically your host/composition root):

```xml
<PropertyGroup>
  <SpaceGenerateRootAggregator>true</SpaceGenerateRootAggregator>
</PropertyGroup>
```

All other handler libraries should either omit this property or set it to `false`:

```xml
<PropertyGroup>
  <SpaceGenerateRootAggregator>false</SpaceGenerateRootAggregator>
</PropertyGroup>
```

### How It Works

- **Root project** generates `DependencyInjectionExtensions.g.cs` with automatic assembly discovery
- **Satellite libraries** generate lightweight `SpaceAssemblyRegistration_<Assembly>.g.cs` files
- At runtime, the root automatically discovers and registers handlers from all referenced assemblies

### Example Structure
```
/MySolution
  src/AppHost/               (root: SpaceGenerateRootAggregator=true)
  src/Features/Users/        (satellite: handlers, pipelines)
  src/Features/Billing/      (satellite: handlers, modules)
  src/Infrastructure/        (satellite: notifications)
```

For complete details, migration guidance, and troubleshooting, see [MultiProjectSetup.md](docs/MultiProjectSetup.md).

---
## Key Features
- Zero runtime reflection for discovery (Roslyn source generator)
- Minimal boilerplate: annotate methods directly with `[Handle]`, `[Pipeline]`, `[Notification]`
- Named handlers (multiple strategies for same request/response)
- Orderable pipelines + early system module execution
- Extensible module model (e.g., cache) before user pipelines
- High-performance async signatures (`ValueTask`)
- Parallel or sequential notification dispatch
- Multi-project root aggregator property `<SpaceGenerateRootAggregator>`
- Multi-targeting (netstandard2.0 + modern .NET)

---
## Performance Philosophy
Space front-loads cost at build time to reduce runtime overhead:
- Compile-time metadata (registrations, maps)
- No reflection-based runtime scanning
- Low allocation pathways (current & planned pooling)

Benchmarks compare against other mediator libraries in `tests/Space.Benchmarks`.

---
## Documentation
Primary docs in `docs/`:

| Topic | Link |
|-------|------|
| Project Overview | [ProjectDoc.en.md](docs/ProjectDoc.en.md) |
| Handlers | [Handlers](docs/Handlers.md) |
| Pipelines | [Pipelines](docs/Pipelines.md) |
| Notifications | [Notifications](docs/Notifications.md) |
| Modules | [Modules](docs/Modules.md) |
| Multi-Project Setup | [MultiProjectSetup](docs/MultiProjectSetup.md) |
| Developer Recommendations | [DeveloperRecommendations](docs/DeveloperRecommendations.md) |
| Known Issues | [KnownIssues](docs/KnownIssues.md) |
| Planned Improvements | [PlannedImprovements](docs/PlannedImprovements.md) |

Per-package:
- [Space.Abstraction](src/Space.Abstraction/README.md)
- [Space.DependencyInjection](src/Space.DependencyInjection/README.md)

---
## Roadmap & Issues
See GitHub Issues for:
- Planned improvements (attribute parameters, global defaults, Options pattern)
- Known issues (initial Lazy `ISpace` null, module scoping bugs)

Contributions welcome via issues & PRs.

---
## Versioning & Releases
- `master`: tagged semantic versions (`vX.Y.Z`) ? stable NuGet
- `dev`: preview releases (`X.Y.Z-preview`)
- Feature branches: validation build only

---
## License
MIT.

---
## Disclaimer
APIs may evolve while early preview features stabilize. Track releases for changes.

---
# Space (Short)

Space is a high-performance, source-generator powered mediator/messaging framework for .NET.

- Docs: see the `docs/` folder
- Contribution guide for modules: see [docs/Contribution.md](docs/Contribution.md)

## Quick links
- Handlers: docs/Handlers.md
- Pipelines: docs/Pipelines.md
- Notifications: docs/Notifications.md
- Known Issues: docs/KnownIssues.md
- Developer Recommendations: docs/DeveloperRecommendations.md

## Build
See `.github/copilot-instructions.md` for environment and common commands.






