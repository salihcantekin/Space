# Space.Abstraction

Core abstractions for the Space framework: high?performance request/response handlers, notifications, pipelines and modular cross?cutting components implemented without runtime reflection (source generator driven).

## Install
```bash
 dotnet add package Space.Abstraction
```

## Goals (Summary)
- Eliminate runtime reflection cost (uses a source generator for compile?time discovery & registration)
- Minimize boilerplate (single `[Handle]` attribute instead of multiple request/handler interfaces)
- Allow multiple related handlers in the same class
- Support named handlers (select concrete handler by name at Send time)
- Provide lightweight pipelines & pluggable system/user modules (e.g. Cache)
- Provide fast notification (publish) model with configurable dispatch strategy

## Key Building Blocks
| Concept | Attribute | Context Parameter | Return | Description |
|---------|-----------|-------------------|--------|-------------|
| Handler | `[Handle]` | `HandlerContext<TRequest>` | `ValueTask<TResponse>` | Core request/response unit |
| Pipeline | `[Pipeline]` | `PipelineContext<TRequest>` | `ValueTask<TResponse>` | Middleware around a handler chain (orderable) |
| Notification | `[Notification]` | `NotificationContext<TEvent>` | `ValueTask`/`Task` | Reacts to published events |
| Module (system) | e.g. `[CacheModule]` | N/A (wraps pipeline) | wraps next | Cross?cutting concern inserted before user pipelines |

## Basic Handler Example
```csharp
public sealed record UserLoginRequest(string UserName);
public sealed record UserLoginResponse(bool Success);

public class UserHandlers
{
    [Handle]
    public ValueTask<UserLoginResponse> Login(HandlerContext<UserLoginRequest> ctx)
    {
        // var userService = ctx.ServiceProvider.GetRequiredService<UserService>();
        // bool success = await userService.Login(ctx.Request);
        bool success = true; // demo
        return ValueTask.FromResult(new UserLoginResponse(success));
    }
}
```

### Named Handlers
Multiple handlers for the same request/response can be differentiated via the `Name` property:
```csharp
public class PricingHandlers
{
    [Handle(Name = "Default")] public ValueTask<PriceResult> GetPrice(HandlerContext<PriceQuery> ctx) => ...;
    [Handle(Name = "Discounted")] public ValueTask<PriceResult> GetDiscounted(HandlerContext<PriceQuery> ctx) => ...;
}
// space.Send<PriceResult>(query, handlerName: "Discounted"); (Send overload in DI package)
```

## Pipelines
Pipelines are middleware components executed around the resolved handler. They can short?circuit or augment behavior.
```csharp
public class UserPipelines
{
    [Pipeline(Order = 1)]
    public async ValueTask<UserLoginResponse> Audit(PipelineContext<UserLoginRequest> ctx)
    {
        // before
        var response = await ctx.Next(ctx);
        // after
        return response;
    }
}
```
`Order` is optional; lower values execute earlier. System modules (e.g. cache) use very low order constants to run before user pipelines.

## Notifications
```csharp
public sealed record UserLoggedInSuccessfully(string UserName);

public class UserLoginNotifications
{
    [Notification]
    public ValueTask LogToFile(NotificationContext<UserLoggedInSuccessfully> ctx)
    {
        // file logging
        return ValueTask.CompletedTask;
    }

    [Notification]
    public ValueTask LogToDb(NotificationContext<UserLoggedInSuccessfully> ctx)
    {
        // db logging
        return ValueTask.CompletedTask;
    }
}
```
Dispatch mode (Parallel / Sequential) is configured via `SpaceOptions.NotificationDispatchType` (see DI package README).

## Modules (Cross?Cutting)
Modules are framework?provided pipeline injectors. Example: Cache module (`Space.Modules.InMemoryCache`) adds caching behavior when a handler method is annotated:
```csharp
public class UserQueries
{
    [Handle]
    [CacheModule(Duration = 60)] // seconds
    public ValueTask<UserDetail?> GetUser(HandlerContext<UserId> ctx) => ...;
}
```
A module attribute does not define its own method; it augments the associated handler.

## Source Generator
The generator scans for the above attributes and emits DI registration / metadata so runtime reflection is avoided.

## Use With Dependency Injection
Add the `Space.DependencyInjection` package and call `services.AddSpace()` (see that package README for details). Then:
```csharp
ISpace space = provider.GetRequiredService<ISpace>();
var response = await space.Send<UserLoginResponse>(new UserLoginRequest("demo"));
await space.Publish(new UserLoggedInSuccessfully("demo"));
```

## Links
- Repo: https://github.com/salihcantekin/Space
- License: MIT
