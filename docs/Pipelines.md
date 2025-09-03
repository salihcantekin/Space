# Pipelines

Pipelines in Space act as middleware for handler execution. They are methods marked with the `[Pipeline]` attribute and can be ordered using the `Order` parameter. Pipelines can perform pre- and post-processing around handler logic.

## Example
```csharp
public partial class UserHandlers
{
    [Pipeline(Order = 1)]
    public async ValueTask<UserLoginResponse> PipelineHandler(PipelineContext<UserLoginRequest> ctx)
    {
        // Before
        var response = await next(ctx);
        // After
        return response;
    }
}
```

You can implement pipeline interfaces for type safety and build-time validation:
```csharp
public class MyPipeline : IPipelineHandler<MyRequest, MyResponse> { ... }
```
