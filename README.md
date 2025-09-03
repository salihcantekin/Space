# Space Framework

High-performance, source-generator powered mediator-style framework.

## Sections
1. Motivation & Goals
2. Packages
3. DI Setup
4. Handlers
5. Pipelines
6. Notifications
7. Modules & Caching
8. Custom Modules
9. Known Issues
10. Planned
11. Suggestions

## 1. Motivation & Goals
Remove runtime reflection, reduce boilerplate, support multiple/named handlers, extensible pipelines & modules, attribute-driven design.

## 2. Packages
Required: Space.DependencyInjection (brings Space.Abstraction).
Optional: Space.Modules.InMemoryCache

## 3. DI Setup
```csharp
services.AddSpace(o => o.NotificationDispatchType = NotificationDispatchType.Parallel);
services.AddSpaceInMemoryCache(); // optional
ISpace space = services.BuildServiceProvider().GetRequiredService<ISpace>();
```

## 4. Handlers
```csharp
public record Login(string UserName);
public record LoginResult(bool Success);
public class AuthHandlers
{
    [Handle]
    public ValueTask<LoginResult> Login(HandlerContext<Login> ctx)
        => ValueTask.FromResult(new LoginResult(true));
}
var res = await space.Send<LoginResult>(new Login("demo"));
```
Named handlers via `[Handle(Name="Alt")]` and `space.Send<Result>(req, name:"Alt")`.

## 5. Pipelines
```csharp
[Pipeline(Order = 1)]
public async ValueTask<LoginResult> Audit(PipelineContext<Login> ctx)
{ var r = await ctx.Next(ctx); return r; }
```

## 6. Notifications
```csharp
public record UserLoggedIn(string UserName);
public class LoginNotifications
{
    [Notification] public ValueTask FileLog(NotificationContext<UserLoggedIn> ctx)=>ValueTask.CompletedTask;
    [Notification] public ValueTask DbLog(NotificationContext<UserLoggedIn> ctx)=>ValueTask.CompletedTask;
}
await space.Publish(new UserLoggedIn("demo"));
```

## 7. Modules & Caching
Annotate handler with module attribute:
```csharp
[Handle]
[CacheModule(Duration=60)]
public ValueTask<UserDetail> Get(HandlerContext<int> ctx) => ...;
```
Register in-memory provider: `services.AddSpaceInMemoryCache();`

## 8. Custom Modules
Create attribute (implements ISpaceModuleAttribute), module class `[SpaceModule(...)]`, config (IModuleConfig), provider (IModuleProvider), DI extension.

## 9. Known Issues
- First handler may see null ISpace due to lazy circular wiring (resolved after first call).
- Module attribute on one of multiple same Req/Res handlers may incorrectly apply to all.

## 10. Planned
- Provider type on attribute `[CacheModule(Provider=typeof(Redis...))]`.
- Global defaults via options.
- Module configs via Options pattern.

## 11. Suggestions
- Per-call notification dispatch override.
- ILoggerFactory integration.

## License
MIT

Repo: https://github.com/salihcantekin/Space


# Space

High-performance, source-generator based mediator-style framework eliminating runtime reflection and reducing boilerplate.

## Quick Install
```bash
dotnet add package Space.DependencyInjection
# optional modules
dotnet add package Space.Modules.InMemoryCache
```

## Basic Usage
```csharp
services.AddSpace(o => o.NotificationDispatchType = NotificationDispatchType.Parallel);
services.AddSpaceInMemoryCache(); // optional
var sp = services.BuildServiceProvider();
var space = sp.GetRequiredService<ISpace>();
var res = await space.Send<LoginResult>(new Login("demo"));
await space.Publish(new UserLoggedInSuccessfully("demo"));
```

## Main Concepts
- [Handle] methods: `ValueTask<TResponse> Method(HandlerContext<TRequest>)`
- [Pipeline] methods: middleware around handlers (optional Order)
- [Notification] methods: event consumers (parallel or sequential dispatch)
- Modules: system pipelines via attributes (e.g. `[CacheModule(Duration=60)]`)
- Named handlers: `[Handle(Name="Alt")]` and `space.Send<TRes>(req, name:"Alt")`

## Documentation
Full English documentation: `docs/ProjectDoc.en.md`
Original Turkish notes: `docs/ProjectDoc.txt`

## Status
Known issues & roadmap inside documentation.

License: MIT
Repository: https://github.com/salihcantekin/Space



