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
| Space.Abstraction | ![NuGet](https://img.shields.io/nuget/v/Space.Abstraction.svg) | ![NuGet (pre)](https://img.shields.io/nuget/vpre/Space.Abstraction.svg) | ![Downloads](https://img.shields.io/nuget/dt/Space.Abstraction.svg) | Core abstractions: attributes, contexts, contracts |
| Space.DependencyInjection | ![NuGet](https://img.shields.io/nuget/v/Space.DependencyInjection.svg) | ![NuGet (pre)](https://img.shields.io/nuget/vpre/Space.DependencyInjection.svg) | ![Downloads](https://img.shields.io/nuget/dt/Space.DependencyInjection.svg) | DI extensions + source generator bootstrap |
| Space.Modules.InMemoryCache | ![NuGet](https://img.shields.io/nuget/v/Space.Modules.InMemoryCache.svg) | ![NuGet (pre)](https://img.shields.io/nuget/vpre/Space.Modules.InMemoryCache.svg) | ![Downloads](https://img.shields.io/nuget/dt/Space.Modules.InMemoryCache.svg) | In-memory caching module + attribute integration |

### Install (Minimal)
```bash
# Add DI (brings Abstraction transitively)
dotnet add package Space.DependencyInjection
```
Or explicitly:
```bash
dotnet add package Space.Abstraction
dotnet add package Space.DependencyInjection
```
Optional module:
```bash
dotnet add package Space.Modules.InMemoryCache
```

---
## Quick Start
```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSpace(opt =>
{
    opt.NotificationDispatchType = NotificationDispatchType.Parallel; // or Sequential
});
services.AddSpaceInMemoryCache(); // if caching module needed

var provider = services.BuildServiceProvider();
var space = provider.GetRequiredService<ISpace>();

public sealed record UserLoginRequest(string UserName);
public sealed record UserLoginResponse(bool Success);

public class UserHandlers
{
    [Handle]
    public ValueTask<UserLoginResponse> Login(HandlerContext<UserLoginRequest> ctx)
        => ValueTask.FromResult(new UserLoginResponse(true));
}

var response = await space.Send<UserLoginResponse>(new UserLoginRequest("demo"));
```

### Named Handlers
```csharp
public class PricingHandlers
{
    [Handle(Name = "Default")] public ValueTask<PriceResult> GetDefault(HandlerContext<PriceQuery> ctx) => ...;
    [Handle(Name = "Discounted")] public ValueTask<PriceResult> GetDiscounted(HandlerContext<PriceQuery> ctx) => ...;
}
var discounted = await space.Send<PriceResult>(new PriceQuery(...), handlerName: "Discounted");
```

### Pipelines
```csharp
public class LoggingPipeline
{
    [Pipeline(Order = 100)]
    public async ValueTask<TResponse> Log<TRequest, TResponse>(PipelineContext<TRequest> ctx)
    {
        // pre
        var result = await ctx.Next(ctx);
        // post
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
    [CacheModule(DurationSeconds = 60)]
    public ValueTask<UserProfile?> GetUser(HandlerContext<UserId> ctx) => ...;
}
```
`[CacheModule]` (from Space.Modules.InMemoryCache) inserts caching logic before user pipelines.

---
## Key Features
- Zero runtime reflection for discovery (Roslyn source generator)
- Minimal boilerplate: annotate methods directly with `[Handle]`, `[Pipeline]`, `[Notification]`
- Named handlers (multiple strategies for same request/response)
- Orderable pipelines + early system module execution
- Extensible module model (e.g., cache) before user pipelines
- High-performance async signatures (`ValueTask`)
- Parallel or sequential notification dispatch
- Multi-targeting (netstandard2.0 + modern .NET)

---
## Performance Philosophy
Space front-loads cost at build time to reduce runtime overhead:
- Compile-time metadata (registrations, maps)
- No reflection-based runtime scanning
- Low allocation pathways (current & planned pooling)

Benchmarks (planned) will compare against other mediator libraries.

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
- Known issues (initial Lazy `ISpace` null, module scoping on named handlers)

Contributions welcome via issues & PRs.

---
## Versioning & Releases
- `master`: tagged semantic versions (`vX.Y.Z`) ? stable NuGet
- `dev`: continuous preview (`X.Y.(Patch+1)-preview.<run>`)
- Feature branches: validation build only

---
## License
MIT.

---
## Disclaimer
APIs may evolve while early preview features stabilize. Track releases for changes.






