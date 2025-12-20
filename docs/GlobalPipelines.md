# Global Pipelines

Global pipelines are a powerful feature in Space that allows you to create cross-cutting concerns that apply to all handlers with matching request and response types. Unlike handler-specific pipelines which must be explicitly attached to each handler, global pipelines automatically execute for every handler with the same `TRequest` and `TResponse`.

## Why Global Pipelines?

Global pipelines are ideal for cross-cutting concerns that should apply uniformly across your application:

- **Exception Handling**: Catch and log exceptions from any handler
- **Validation**: Validate requests before they reach handlers
- **Logging/Auditing**: Log all requests and responses
- **Performance Monitoring**: Measure execution time
- **Caching**: Cache responses based on request data
- **Authorization**: Check permissions before executing handlers

## Basic Usage

### Creating a Global Pipeline

Mark a method with `[GlobalPipeline]` attribute. The method signature is identical to regular pipelines:

```csharp
public class ExceptionHandlerPipeline
{
    [GlobalPipeline(Order = 10)]
    public async ValueTask<TResponse> HandleExceptions<TRequest, TResponse>(
        PipelineContext<TRequest> ctx,
        PipelineDelegate<TRequest, TResponse> next)
    {
        try
        {
            return await next(ctx);
        }
        catch (Exception ex)
        {
            // Log exception
            Console.WriteLine($"Error handling {typeof(TRequest).Name}: {ex.Message}");
            throw;
        }
    }
}
```

### Using IGlobalPipeline Interface (Recommended)

For compile-time type safety, implement the `IGlobalPipeline<TRequest, TResponse>` interface:

```csharp
public class ValidationPipeline<TRequest, TResponse> : IGlobalPipeline<TRequest, TResponse>
    where TRequest : IValidatable
{
    [GlobalPipeline(Order = 5)]
    public async ValueTask<TResponse> HandleGlobalPipeline(
        PipelineContext<TRequest> ctx,
        PipelineDelegate<TRequest, TResponse> next)
    {
        // Validate request
        var errors = ctx.Request.Validate();
        if (errors.Any())
        {
            throw new ValidationException(errors);
        }

        return await next(ctx);
    }
}
```

**Note:** The `IGlobalPipeline<TRequest, TResponse>` interface is optional. The `[GlobalPipeline]` attribute is required for discovery by the source generator. The interface provides compile-time enforcement of the method signature.

## Execution Stages

Global pipelines can be configured to execute at different stages relative to handler-specific pipelines using the `ExecutionStage` property:

### BeforeHandler (Default, Stage 0)

Executes before any handler-specific pipelines. This is the outermost layer.

```csharp
[GlobalPipeline(Order = 10, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
public async ValueTask<TResponse> Logging<TRequest, TResponse>(
    PipelineContext<TRequest> ctx,
    PipelineDelegate<TRequest, TResponse> next)
{
    Console.WriteLine($"Request: {typeof(TRequest).Name}");
    var response = await next(ctx);
    Console.WriteLine($"Response: {typeof(TResponse).Name}");
    return response;
}
```

**Execution Order:**
1. Global pipelines (BeforeHandler)
2. Handler-specific pipelines
3. Handler
4. Unwinding handler-specific pipelines
5. Unwinding global pipelines

### BeforeHandlerInner (Stage 1)

Executes after handler-specific pipelines but before the handler itself.

```csharp
[GlobalPipeline(Order = 100, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandlerInner)]
public async ValueTask<TResponse> FinalValidation<TRequest, TResponse>(
    PipelineContext<TRequest> ctx,
    PipelineDelegate<TRequest, TResponse> next)
{
    // Final validation right before handler
    return await next(ctx);
}
```

**Execution Order:**
1. Handler-specific pipelines
2. Global pipelines (BeforeHandlerInner)
3. Handler
4. Unwinding global pipelines
5. Unwinding handler-specific pipelines

### AfterHandlerInner (Stage 2)

Executes immediately after the handler, before unwinding handler-specific pipelines.

```csharp
[GlobalPipeline(Order = 100, ExecutionStage = GlobalPipelineExecutionStage.AfterHandlerInner)]
public async ValueTask<TResponse> PostProcessing<TRequest, TResponse>(
    PipelineContext<TRequest> ctx,
    PipelineDelegate<TRequest, TResponse> next)
{
    var response = await next(ctx);
    // Post-process the handler response
    return response;
}
```

### AfterHandler (Stage 3)

Executes after all handler-specific pipelines have unwound. This is the outermost post-handler layer.

```csharp
[GlobalPipeline(Order = 200, ExecutionStage = GlobalPipelineExecutionStage.AfterHandler)]
public async ValueTask<TResponse> ResponseCaching<TRequest, TResponse>(
    PipelineContext<TRequest> ctx,
    PipelineDelegate<TRequest, TResponse> next)
{
    var response = await next(ctx);
    // Cache the final response
    return response;
}
```

## Ordering Within Stages

Within each execution stage, global pipelines are ordered by the `Order` property (lower values execute first):

```csharp
public class OrderedGlobalPipelines
{
    [GlobalPipeline(Order = 10, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
    public async ValueTask<TResponse> First<TRequest, TResponse>(...)
    {
        // Executes first (outer)
        return await next(ctx);
    }

    [GlobalPipeline(Order = 20, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
    public async ValueTask<TResponse> Second<TRequest, TResponse>(...)
    {
        // Executes second (inner, closer to handler)
        return await next(ctx);
    }
}
```

## Complete Execution Flow Example

```csharp
public record UserRequest(string UserId) : IRequest<UserResponse>;
public record UserResponse(string Name);

public class UserHandler
{
    [Handle]
    public ValueTask<UserResponse> Handle(HandlerContext<UserRequest> ctx)
    {
        return ValueTask.FromResult(new UserResponse("John"));
    }

    [Pipeline(Order = 50)]
    public async ValueTask<UserResponse> HandlerPipeline(
        PipelineContext<UserRequest> ctx,
        PipelineDelegate<UserRequest, UserResponse> next)
    {
        Console.WriteLine("Handler Pipeline - Before");
        var result = await next(ctx);
        Console.WriteLine("Handler Pipeline - After");
        return result;
    }
}

public class GlobalPipelines
{
    [GlobalPipeline(Order = 10, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
    public async ValueTask<UserResponse> GP1(
        PipelineContext<UserRequest> ctx,
        PipelineDelegate<UserRequest, UserResponse> next)
    {
        Console.WriteLine("Global Pipeline 1 - Before");
        var result = await next(ctx);
        Console.WriteLine("Global Pipeline 1 - After");
        return result;
    }

    [GlobalPipeline(Order = 100, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandlerInner)]
    public async ValueTask<UserResponse> GP2(
        PipelineContext<UserRequest> ctx,
        PipelineDelegate<UserRequest, UserResponse> next)
    {
        Console.WriteLine("Global Pipeline 2 - Before");
        var result = await next(ctx);
        Console.WriteLine("Global Pipeline 2 - After");
        return result;
    }
}

// Execution output:
// Global Pipeline 1 - Before
// Handler Pipeline - Before
// Global Pipeline 2 - Before
// Handler executes
// Global Pipeline 2 - After
// Handler Pipeline - After
// Global Pipeline 1 - After
```

## Type Matching

Global pipelines only execute for handlers with **exact type match** on both `TRequest` and `TResponse`:

```csharp
public record RequestA(int Id) : IRequest<ResponseA>;
public record ResponseA(string Value);

public record RequestB(int Id) : IRequest<ResponseB>;
public record ResponseB(string Value);

public class TypedGlobalPipeline
{
    // Only executes for RequestA -> ResponseA
    [GlobalPipeline]
    public async ValueTask<ResponseA> OnlyForA(
        PipelineContext<RequestA> ctx,
        PipelineDelegate<RequestA, ResponseA> next)
    {
        return await next(ctx);
    }
}
```

## Performance Considerations

### Zero Overhead When Disabled

When no global pipelines are registered in the system, there is **zero performance overhead**. The Space framework detects this at build time and generates optimized code paths that bypass global pipeline checks entirely.

### Minimal Overhead When Enabled

Global pipelines are composed into the handler execution chain at registration time, not runtime. This means:

- **No reflection** at runtime
- **No dynamic dispatch**
- **Single composed delegate** for the entire pipeline chain
- **Inline execution** similar to regular pipelines

Benchmarks show that global pipelines add minimal overhead (typically <1% compared to handlers without any pipelines).

## Best Practices

### 1. Use ExecutionStage Wisely

- **BeforeHandler**: For cross-cutting concerns that should wrap everything (logging, metrics, exception handling)
- **BeforeHandlerInner**: For final pre-processing before the handler (authorization, validation)
- **AfterHandlerInner**: For immediate post-processing of handler results
- **AfterHandler**: For final response transformation, caching, or cleanup

### 2. Keep Global Pipelines Generic

Design global pipelines to work with any request/response type where possible:

```csharp
public class GenericLoggingPipeline
{
    [GlobalPipeline(Order = 1)]
    public async ValueTask<TResponse> Log<TRequest, TResponse>(
        PipelineContext<TRequest> ctx,
        PipelineDelegate<TRequest, TResponse> next)
    {
        var logger = ctx.ServiceProvider.GetRequiredService<ILogger>();
        logger.LogInformation($"Handling {typeof(TRequest).Name}");
        
        try
        {
            var response = await next(ctx);
            logger.LogInformation($"Completed {typeof(TRequest).Name}");
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed {typeof(TRequest).Name}");
            throw;
        }
    }
}
```

### 3. Use Constraints for Specialized Pipelines

When you need type-specific behavior, use generic constraints:

```csharp
public class ValidationPipeline<TRequest, TResponse>
    where TRequest : IValidatable
{
    [GlobalPipeline(Order = 5)]
    public async ValueTask<TResponse> Validate(
        PipelineContext<TRequest> ctx,
        PipelineDelegate<TRequest, TResponse> next)
    {
        ctx.Request.Validate(); // Only works with IValidatable
        return await next(ctx);
    }
}
```

### 4. Order Matters

Choose Order values that leave room for expansion:
- 1-10: Outermost cross-cutting concerns (logging, exception handling)
- 20-50: Authentication/Authorization
- 60-90: Validation
- 100+: Handler-specific pipelines (default)

### 5. Avoid State Mutation

Global pipelines execute for all matching handlers. Avoid storing handler-specific state in the pipeline class:

```csharp
// ? Bad: Instance state
public class StatefulPipeline
{
    private int callCount = 0; // Shared across all handlers!
    
    [GlobalPipeline]
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(...)
    {
        callCount++; // Race condition!
        // ...
    }
}

// ? Good: Use PipelineContext.Items for per-request state
public class StatelessPipeline
{
    [GlobalPipeline]
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(...)
    {
        var startTime = DateTime.UtcNow;
        var response = await next(ctx);
        var duration = DateTime.UtcNow - startTime;
        // Log duration...
        return response;
    }
}
```

## Common Patterns

### Exception Handling

```csharp
public class GlobalExceptionHandler
{
    [GlobalPipeline(Order = 1, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
    public async ValueTask<TResponse> HandleExceptions<TRequest, TResponse>(
        PipelineContext<TRequest> ctx,
        PipelineDelegate<TRequest, TResponse> next)
    {
        try
        {
            return await next(ctx);
        }
        catch (ValidationException ex)
        {
            // Log and transform to error response
            var logger = ctx.ServiceProvider.GetRequiredService<ILogger>();
            logger.LogWarning(ex, "Validation failed for {RequestType}", typeof(TRequest).Name);
            throw;
        }
        catch (Exception ex)
        {
            var logger = ctx.ServiceProvider.GetRequiredService<ILogger>();
            logger.LogError(ex, "Unhandled exception in {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }
}
```

### Performance Monitoring

```csharp
public class PerformanceMonitoringPipeline
{
    [GlobalPipeline(Order = 2)]
    public async ValueTask<TResponse> Monitor<TRequest, TResponse>(
        PipelineContext<TRequest> ctx,
        PipelineDelegate<TRequest, TResponse> next)
    {
        var metrics = ctx.ServiceProvider.GetRequiredService<IMetricsCollector>();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await next(ctx);
            stopwatch.Stop();
            metrics.RecordSuccess(typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch
        {
            stopwatch.Stop();
            metrics.RecordFailure(typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### Request/Response Logging

```csharp
public class RequestResponseLoggingPipeline
{
    [GlobalPipeline(Order = 5)]
    public async ValueTask<TResponse> LogRequestResponse<TRequest, TResponse>(
        PipelineContext<TRequest> ctx,
        PipelineDelegate<TRequest, TResponse> next)
    {
        var logger = ctx.ServiceProvider.GetRequiredService<ILogger>();
        var serializer = ctx.ServiceProvider.GetRequiredService<IJsonSerializer>();
        
        var requestJson = serializer.Serialize(ctx.Request);
        logger.LogDebug("Request: {RequestType} = {RequestJson}", 
            typeof(TRequest).Name, requestJson);
        
        var response = await next(ctx);
        
        var responseJson = serializer.Serialize(response);
        logger.LogDebug("Response: {ResponseType} = {ResponseJson}", 
            typeof(TResponse).Name, responseJson);
        
        return response;
    }
}
```

## Diagnostics

The Space source generator validates global pipeline signatures at build time:

### With IGlobalPipeline Interface

If your class implements `IGlobalPipeline<TRequest, TResponse>`, the compiler enforces the signature automatically.

### Without IGlobalPipeline Interface

If you use only the `[GlobalPipeline]` attribute, the source generator validates:

- **GLOBALPIPELINE020**: Method must have exactly two parameters
- **GLOBALPIPELINE021**: First parameter must be `PipelineContext<TRequest>`
- **GLOBALPIPELINE022**: Second parameter must be `PipelineDelegate<TRequest, TResponse>`
- **GLOBALPIPELINE023**: Return type must be `Task<TResponse>` or `ValueTask<TResponse>`
- **GLOBALPIPELINE024**: TRequest type mismatch between parameters
- **GLOBALPIPELINE025**: TResponse type mismatch between parameters

## Summary

Global pipelines provide a powerful way to implement cross-cutting concerns without repeating code across handlers. They:

- Execute automatically for all handlers with matching request/response types
- Support flexible execution stages relative to handler-specific pipelines
- Have zero overhead when not used
- Are validated at build time for type safety
- Integrate seamlessly with the existing pipeline infrastructure

Use global pipelines for truly cross-cutting concerns, and handler-specific pipelines for behavior unique to individual handlers.
