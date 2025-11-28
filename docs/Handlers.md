# Handlers

Handlers in Space are methods marked with the `[Handle]` attribute. They process requests and return responses asynchronously. Handlers can be grouped in classes and can use attribute parameters for naming and modular configuration.

> **Important:** The `[Handle]` attribute is mandatory for handler discovery by the source generator. The optional `IHandler<TRequest, TResponse>` interface can be implemented for type safety hints, but does not register the handler on its own.

> Multi-Project Note: If handlers live in multiple class libraries, ensure exactly one project sets `<SpaceGenerateRootAggregator>true</SpaceGenerateRootAggregator>` so that all satellite libraries' handlers are discovered. See `MultiProjectSetup.md`.

## Example
```csharp
public record UserLoginRequest(string UserName) : IRequest<UserLoginResponse>;
public record UserLoginResponse(bool Success);

public partial class UserHandlers
{
    [Handle]
    public async ValueTask<UserLoginResponse> Login(HandlerContext<UserLoginRequest> ctx)
    {
        var userService = ctx.ServiceProvider.GetService<UserService>();
        var userExists = await userService.Login(ctx.Request);
        return new UserLoginResponse(userExists);
    }
}
```

## Usage
Preferred strongly-typed usage:
```csharp
ISpace space = serviceProvider.GetRequiredService<ISpace>();
var loginResponse = await space.Send<UserLoginRequest, UserLoginResponse>(new UserLoginRequest("sc"));
```

Additional overloads:
- IRequest overload: `await space.Send<UserLoginResponse>(IRequest<UserLoginResponse> request, string? name = null)`
- Object overload: `await space.Send<UserLoginResponse>(object request, string? name = null)`
- Void-like (non-generic) convenience: `await space.Send(object request, string? name = null)` -> internally `Send<Nothing>`

> Rule: In `Send<TRequest, TResponse>`, `TRequest` must implement `IRequest<TResponse>`. This is enforced at compile time unless you intentionally dispatch with different concrete types via object overload.

Handlers can be named using the `Name` parameter and invoked by name:
```csharp
[Handle(Name = "CustomHandler")]
public ValueTask<ResponseType> CustomHandler(HandlerContext<RequestType> ctx) { ... }

var response = await space.Send<RequestType, ResponseType>(new RequestType(...), name: "CustomHandler");
```

## Selecting default handler with IsDefault
When multiple handlers exist for the same request/response pair, you can mark one as the default so that `Send` without a name uses it.

```csharp
public record PriceQuery(int Id) : IRequest<PriceResult>;
public record PriceResult(string Tag);

public class PricingHandlers
{
    [Handle(Name = "Standard")]
    public ValueTask<PriceResult> Standard(HandlerContext<PriceQuery> ctx)
        => ValueTask.FromResult(new PriceResult("STD"));

    [Handle(Name = "Discounted", IsDefault = true)]
    public ValueTask<PriceResult> Discounted(HandlerContext<PriceQuery> ctx)
        => ValueTask.FromResult(new PriceResult("DISC"));
}

// Without name -> uses the IsDefault handler (Discounted)
var r1 = await space.Send<PriceQuery, PriceResult>(new PriceQuery(1));
// With name -> uses the specified handler
var r2 = await space.Send<PriceQuery, PriceResult>(new PriceQuery(2), name: "Standard");
```

Rules:
- Only one handler per (Request, Response) pair can be marked `IsDefault = true`. The source generator emits a diagnostic if more than one is found.
- If no handler is marked as default, the last discovered handler remains the fallback for unnamed sends.

## Void-like Handlers (Non-generic Task/ValueTask)
If a handler returns non-generic `Task` or `ValueTask`, Space normalizes the result to `Nothing` automatically.

```csharp
public record CreateOp(string Tag) : IRequest<Nothing>;

public class CreateHandlers
{
    [Handle]
    public ValueTask Perform(HandlerContext<CreateOp> ctx) => ValueTask.CompletedTask; // -> ValueTask<Nothing>

    [Handle(Name = "Alt")]
    public Task PerformAlt(HandlerContext<CreateOp> ctx) => Task.CompletedTask;        // -> Task<Nothing>
}

await space.Send<CreateOp, Nothing>(new CreateOp("x"));
await space.Send(new CreateOp("y"), name: "Alt"); // non-generic Send
```

Diagnostic rule `HANDLE014` prevents using non-generic return when `TRequest : IRequest<TNonNothing>`.

See `VoidLikeHandlers.md` for deeper details.
