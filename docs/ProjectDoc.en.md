# Space Project Documentation (English)

## 1. Purpose
The goal of this project is to build a high-performance alternative to MediatR while removing / minimizing several usability and performance drawbacks:
- Eliminate runtime reflection (a major MediatR cost) by using a source generator for compile-time discovery & registration.
- Reduce interface & model boilerplate (no need for separate IRequest / IRequestHandler pairs per request unless you prefer helper interfaces).
- Allow multiple related handlers to co-exist in a single class via simple attributes.
- Introduce an extensible Module system (e.g. Caching) implemented as system pipelines that run before user pipelines.
- Support named handlers so multiple handlers for the same Request/Response can be selected explicitly at Send time.
- Multi-project handler discovery via a single root aggregator + per-assembly lightweight registration (see `MultiProjectSetup.md`).

## 2. Required NuGet Packages
Core usage requires referencing BOTH packages explicitly:
- `Space.Abstraction` (automatically includes the Space.SourceGenerator analyzer; install this and you get compile-time registration)
- `Space.DependencyInjection` (runtime DI extensions + implementations of ISpace and registries)

Explicit requirement for multi-project solutions:
- Root aggregator project (the one with `<SpaceGenerateRootAggregator>true</SpaceGenerateRootAggregator>`) MUST reference BOTH `Space.Abstraction` and `Space.DependencyInjection`.
- Satellite class libraries that only declare handlers/pipelines/notifications SHOULD reference `Space.Abstraction` (to bring the analyzer). They typically do NOT need `Space.DependencyInjection`.

Optional modules (examples):
- `Space.Modules.InMemoryCache`

> Previous versions auto-brought abstractions via `Space.DependencyInjection`. This changed so any project that uses attributes (even without DI) can get source generation by referencing `Space.Abstraction` alone.

### Breaking change (packaging)
- Old: `Space.DependencyInjection` brought abstractions + analyzer implicitly.
- New: `Space.Abstraction` brings the analyzer. `Space.DependencyInjection` provides runtime DI only.
- Migration:
  - If you referenced only `Space.DependencyInjection`, add `Space.Abstraction` to every project that uses Space attributes.
  - Remove any explicit `Space.SourceGenerator` project/NuGet references; they are no longer necessary when `Space.Abstraction` is present.

## 3. Dependency Injection
```csharp
services.AddSpace(opt =>
{
    opt.NotificationDispatchType = NotificationDispatchType.Parallel; // or Sequential
});

// Optional module provider(s)
services.AddSpaceInMemoryCache();

var provider = services.BuildServiceProvider();
ISpace space = provider.GetRequiredService<ISpace>();
```

### 3.1 Multi-Project Root Aggregator Configuration
Add to exactly ONE project (host / composition root):
```xml
<PropertyGroup>
  <SpaceGenerateRootAggregator>true</SpaceGenerateRootAggregator>
</PropertyGroup>
```
And ensure the root aggregator project references BOTH packages:
```xml
<ItemGroup>
  <PackageReference Include="Space.Abstraction" Version="X.Y.Z" />
  <PackageReference Include="Space.DependencyInjection" Version="X.Y.Z" />
</ItemGroup>
```
All other handler libraries should either omit the property or set it to false and reference only `Space.Abstraction`:
```xml
<PropertyGroup>
  <SpaceGenerateRootAggregator>false</SpaceGenerateRootAggregator>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Space.Abstraction" Version="X.Y.Z" />
</ItemGroup>
```
See `MultiProjectSetup.md` for full rationale and migration guidance.

## 4. Handler Implementation
A handler is a method annotated with `[Handle]` taking `HandlerContext<TRequest>` and returning `ValueTask<TResponse>`.
```csharp
public sealed record UserLoginRequest(string UserName);
public sealed record UserLoginResponse(bool Success);

public class UserHandlers
{
    [Handle]
    public async ValueTask<UserLoginResponse> Login(HandlerContext<UserLoginRequest> ctx)
    {
        var userService = ctx.ServiceProvider.GetService<UserService>();
        var loginModel = ctx.Request;
        bool userExists = true; // demo
        return new UserLoginResponse(userExists);
    }
}

var loginResponse = await space.Send<UserLoginResponse>(new UserLoginRequest("sc"));
```
You may optionally implement helper interfaces (e.g. `IHandler<TReq,TRes>`) for type safety hints, but the `[Handle]` attribute is mandatory for handler discovery. The interface alone does not register the handler.

### Named Handlers
Provide multiple handlers for same Request/Response:
```csharp
public class PricingHandlers
{
    [Handle(Name = "Default")] public ValueTask<PriceResult> Get(HandlerContext<PriceQuery> ctx) => ...;
    [Handle(Name = "Discounted")] public ValueTask<PriceResult> GetDiscount(HandlerContext<PriceQuery> ctx) => ...;
}
var discounted = await space.Send<PriceResult>(new PriceQuery(...), name: "Discounted");
```

## 5. Pipelines
Pipelines are middleware around handlers.
```csharp
public class UserPipelines
{
    [Pipeline(Order = 1)]
    public async ValueTask<UserLoginResponse> PipelineHandler(PipelineContext<UserLoginRequest> ctx, PipelineDelegate<UserLoginRequest, UserLoginResponse> next)
    {
        var response = await next(ctx);
        return response;
    }
}
```
Lower `Order` executes earlier. System Modules use very low (negative) orders to run before user pipelines.

## 6. Notifications
Notification handlers react to published events.
```csharp
services.AddSpace(opt =>
{
    opt.NotificationDispatchType = NotificationDispatchType.Parallel; // or Sequential
});

public sealed record UserLoggedInSuccessfully(string UserName);

public class UserHandlersNotifications
{
    [Notification]
    public ValueTask LoginNotificationHandlerForFileLogging(NotificationContext<UserLoggedInSuccessfully> ctx)
    { return ValueTask.CompletedTask; }

    [Notification]
    public ValueTask LoginNotificationHandlerForDbLogging(NotificationContext<UserLoggedInSuccessfully> ctx)
    { return ValueTask.CompletedTask; }
}

await space.Publish(new UserLoggedInSuccessfully("sc"));
```

## 7. Modules (System Pipelines)
Modules are system-provided pipeline layers triggered by decorating a handler method with a module attribute.

```csharp
public sealed record UserDetail(string FullName, string EmailAddress);

public class UserHandlers
{
    [Handle]
    [CacheModule(Duration = 60)]
    public ValueTask<List<UserDetail>> GetUserDetails(HandlerContext<int> ctx)
    {
        var userService = ctx.ServiceProvider.GetService<UserService>();
        return ValueTask.FromResult(new List<UserDetail>());
    }
}

services.AddSpaceInMemoryCache();
```
### Custom Redis Provider (Concept)
```csharp
public sealed class RedisCacheModuleProvider : ICacheModuleProvider
{
    private readonly ConcurrentDictionary<string, object> storage = new();

    public string GetKey<TRequest>(TRequest request) => request?.ToString();
    public ValueTask Store<TResponse>(string key, TResponse resp, CacheModuleConfig cfg) { storage[key] = resp; return default; }
    public bool TryGet<TResponse>(string key, out TResponse resp, CacheModuleConfig cfg)
    { resp = default; if(!storage.TryGetValue(key, out var o)) return false; resp = (TResponse)o; return true; }
}
```
Register it instead of the default in-memory provider.

## 8. Custom Module Implementation Guidelines
When adding a new module:
1. Create an attribute implementing `ISpaceModuleAttribute`.
2. Create a `SpaceModule` subclass decorated with `[SpaceModule(ModuleAttributeType = typeof(YourModuleAttribute))]`.
3. Provide a config model implementing `IModuleConfig`.
4. Provide a provider implementing `IModuleProvider`.
5. Offer an extension method to register provider/config.
6. Pick a `PipelineOrder` relative to other system modules.

## 9. Known Issues
- `ISpace` & `SpaceRegistry` circular dependency (first handler may see null lazy ISpace).
- Module scoping for named handlers still being refined.

## 10. Planned Improvements
- Attribute-level provider specification.
- Global default module configuration.
- Options pattern for module configs.

## 11. Suggestions (Future)
- Integrate `ILoggerFactory` for built-in logging.

## 12. Summary
Space provides a lean, attribute & source-generator driven mediator approach with extensible modules and zero runtime reflection registration cost.

## 13. Versioning & Releases
Uses GitHub Releases for publish.
- Stable: normal release (`vX.Y.Z`)
- Preview: pre-release (`vX.Y.Z-preview`)
- Validation: branch/PR build

## 14. Multi-Project Setup Reference
See `MultiProjectSetup.md`.

## License
MIT
