# Space Project Documentation (English)

## 1. Purpose
The goal of this project is to build a high?performance alternative to MediatR while removing / minimizing several usability and performance drawbacks:
- Eliminate runtime reflection (a major MediatR cost) by using a Source Generator for compile?time discovery & registration.
- Reduce interface & model boilerplate (no need for separate IRequest / IRequestHandler pairs per request unless you prefer helper interfaces).
- Allow multiple related handlers to co?exist in a single class via simple attributes.
- Introduce an extensible Module system (e.g. Caching) implemented as system pipelines that run before user pipelines.
- Support named handlers so multiple handlers for the same Request/Response can be selected explicitly at Send time.

## 2. Required NuGet Packages
Core usage requires referencing:
- `Space.DependencyInjection` (brings registration extensions & analyzer)
- `Space.Abstraction`

Optional modules (examples):
- `Space.Modules.InMemoryCache`

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
        // var userExists = await userService.Login(loginModel);
        bool userExists = true; // demo
        return new UserLoginResponse(userExists);
    }
}

var loginResponse = await space.Send<UserLoginResponse>(new UserLoginRequest("sc"));
```
You may optionally implement helper interfaces (e.g. `IHandler<TReq,TRes>`) if the project provides them, but attributes are sufficient.

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
    [Pipeline(Order = 1)] // Order is optional
    public async ValueTask<UserLoginResponse> PipelineHandler(PipelineContext<UserLoginRequest> ctx)
    {
        // Before
        var response = await ctx.Next(ctx);
        // After
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
    { /* log to file */ return ValueTask.CompletedTask; }

    [Notification]
    public ValueTask LoginNotificationHandlerForDbLogging(NotificationContext<UserLoggedInSuccessfully> ctx)
    { /* log to DB */ return ValueTask.CompletedTask; }
}

await space.Publish(new UserLoggedInSuccessfully("sc"));
```
Dispatch strategy (Parallel / Sequential) is configured globally. You can also override it per call:
```csharp
// Override globally configured strategy just for this call
await space.Publish(new UserLoggedInSuccessfully("sc"), NotificationDispatchType.Parallel);
await space.Publish(new UserLoggedInSuccessfully("sc"), NotificationDispatchType.Sequential);
```

## 7. Modules (System Pipelines)
Modules are system-provided pipeline layers triggered by decorating a handler method with a module attribute. Example: caching.

> NOTE: The attribute name in code is currently `CacheModuleAttribute` (use `[CacheModule(...)]`). Older docs might show `[Cache]`.

```csharp
public sealed record UserDetail(string FullName, string EmailAddress);

public class UserHandlers
{
    [Handle]
    [CacheModule(Duration = 60)] // seconds
    public ValueTask<List<UserDetail>> GetUserDetails(HandlerContext<int> ctx)
    {
        var userService = ctx.ServiceProvider.GetService<UserService>();
        return ValueTask.FromResult(new List<UserDetail>());
    }
}

services.AddSpaceInMemoryCache();
```
### Custom (Concept) Redis Provider
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
1. Create an attribute implementing `ISpaceModuleAttribute` (e.g. `[AuditModule]`).
2. Create a `SpaceModule` subclass decorated with `[SpaceModule(ModuleAttributeType = typeof(AuditModuleAttribute))]`.
3. Provide a config model implementing `IModuleConfig`.
4. Provide a provider implementing `IModuleProvider` (and custom interfaces if needed, e.g. `IAuditModuleProvider`).
5. Offer an extension method to register provider/config (e.g. `AddSpaceAudit`).
6. Pick a `PipelineOrder` relative to other system modules (e.g. Audit before Cache, both before user pipelines).

Module attributes do not define their own methods; they annotate existing `[Handle]` methods.

## 9. Known Issues
- `ISpace` & `SpaceRegistry` circular dependency: the lazy `ISpace` may be null in the very first handler instance; populated on subsequent requests.
- If multiple handlers share the same request/response and only one has a module attribute, the module may still apply to all (scoping bug to refine).

## 10. Planned Improvements
- Allow specifying provider type directly on module attributes: `[CacheModule(Provider = typeof(RedisCacheModuleProvider))]`.
- Global default module parameter configuration: `services.AddCache(opt => opt.Duration = TimeSpan.FromHours(1));`
- Register module configs via the Options pattern so providers can access application-wide defaults.

## 11. Suggestions (Future)
- Integrate `ILoggerFactory` for built-in logging across pipelines/modules.

## 12. Summary
Space provides a lean, attribute & source-generator driven approach to mediator patterns with extensible, modular cross-cutting pipelines and zero runtime reflection registration cost.

## 13. Versioning & Releases
This repository uses GitHub Releases to drive NuGet publishing. No packages are published on branch pushes.

- Stable release (prod publish):
  - Create a GitHub Release and DO NOT mark it as "pre-release".
  - Tag format: either `vX.Y.Z` or `X.Y.Z` (a single leading `v` is ignored).
  - The Prod workflow builds, tests, packs, and publishes packages with version `X.Y.Z`.

- Preview release (dev publish):
  - Create a GitHub Release and mark it as "pre-release".
  - Tag format: `vX.Y.Z` or `X.Y.Z`.
  - The Dev workflow builds, tests, packs, and publishes packages with version `X.Y.Z-preview`.

- Validation CI:
  - Feature branches (`feature/*`, `features/*`) run build/test validation on push.
  - Pull requests into `dev` or `master` run validation as well.

- Version parsing details:
  - A single leading `v`/`V` is trimmed (e.g., `v1.2.3` -> `1.2.3`).
  - Basic SemVer validation is performed. Pre-release/build metadata are supported.

- Local development:
  - Debug builds use a local version like `0.0.0-local` to avoid colliding with published packages.

## License
MIT
