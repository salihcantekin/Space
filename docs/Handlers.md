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
