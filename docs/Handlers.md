# Handlers

Handlers in Space are methods marked with the `[Handle]` attribute. They process requests and return responses asynchronously. Handlers can be grouped in classes and can use attribute parameters for naming and modular configuration.

## Example
```csharp
public record UserLoginRequest(string UserName);
public record UserLoginResponse(bool Success);

public partial class UserHandlers
{
    [Handle]
    public async ValueTask<UserLoginResponse> Login(HandlerContext<UserLoginRequest> ctx)
    {
        var userService = ctx.ServiceProvider.GetService<UserService>();
        var loginModel = ctx.Request;
        var userExists = await userService.Login(loginModel);
        return new UserLoginResponse() { Success = userExists };
    }
}
```

## Usage
```csharp
ISpace space = serviceProvider.GetRequiredService<ISpace>();
var loginResponse = await space.Send<UserLoginResponse>(new UserLoginRequest { UserName = "sc" });
```

Handlers can be named using the `Name` parameter and invoked by name:
```csharp
[Handle(Name = "CustomHandler")]
public ValueTask<ResponseType> CustomHandler(HandlerContext<RequestType> ctx) { ... }

var response = await space.Send<ResponseType>(request, name: "CustomHandler");
```

## Selecting default handler with IsDefault
When multiple handlers exist for the same request/response pair, you can mark one as the default so that `Send` without a name uses it.

```csharp
public record PriceQuery(int Id);
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
