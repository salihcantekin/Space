# Pipelines

Pipelines in Space act as middleware for handler execution. They are methods marked with the `[Pipeline]` attribute and can be ordered using the `Order` parameter. Pipelines can perform pre- and post-processing around handler logic.

## Example
```csharp
public record UserLoginRequest(string UserName) : IRequest<UserLoginResponse>;
public record UserLoginResponse(bool Success);

public partial class UserHandlers
{
    // Runs first (lower Order executes earlier in the chain)
    [Pipeline(Order = 1)]
    public async ValueTask<UserLoginResponse> First(PipelineContext<UserLoginRequest> ctx, PipelineDelegate<UserLoginRequest, UserLoginResponse> next)
    {
        // Before handler / next pipelines
        var response = await next(ctx);
        // After handler / next pipelines
        return response;
    }

    // Runs after First (higher Order wraps closer to the handler)
    [Pipeline(Order = 2)]
    public async ValueTask<UserLoginResponse> Second(PipelineContext<UserLoginRequest> ctx, PipelineDelegate<UserLoginRequest, UserLoginResponse> next)
    {
        var response = await next(ctx);
        return response;
    }
}
```

You can implement pipeline interfaces for type safety and build-time validation:
```csharp
public class MyPipeline : IPipelineHandler<MyRequest, MyResponse>
    where MyRequest : IRequest<MyResponse>
{
    public ValueTask<MyResponse> HandlePipeline(PipelineContext<MyRequest> ctx, PipelineDelegate<MyRequest, MyResponse> next)
        => next(ctx);
}
```

## Sharing data across pipelines with PipelineContext.Items
`PipelineContext<TRequest>` provides a lightweight item bag accessible by all pipelines in the same execution chain via `SetItem` and `GetItem`.

- Items set in an earlier pipeline are visible in subsequent pipelines.
- Items are cleared automatically between requests.

```csharp
public record MyReq(string Text) : IRequest<MyRes>;
public record MyRes(string Text);

public class SampleHandlers
{
    // Global pipelines (no handle name) for the same request/response

    // Order = 1 executes first
    [Pipeline(Order = 1)]
    public async ValueTask<MyRes> P1(PipelineContext<MyReq> ctx, PipelineDelegate<MyReq, MyRes> next)
    {
        // Make data available to next pipelines
        ctx.SetItem("trace-id", Guid.NewGuid().ToString());
        var res = await next(ctx);
        // Read again later if needed
        var tid = (string)ctx.GetItem("trace-id");
        return res with { Text = $"{res.Text}:P1={tid}" };
    }

    // Order = 2 executes after P1 (closer to the handler)
    [Pipeline(Order = 2)]
    public async ValueTask<MyRes> P2(PipelineContext<MyReq> ctx, PipelineDelegate<MyReq, MyRes> next)
    {
        // Access value set by P1
        var tid = (string)ctx.GetItem("trace-id");
        var res = await next(ctx);
        return res with { Text = $"{res.Text}:P2={tid}" };
    }
}
```

This mirrors the unit test behavior where one pipeline sets an item and the next pipeline reads it, confirming shared access through `PipelineContext`.
