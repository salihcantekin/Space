using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;

namespace Space.TestConsole.Pipelines;

/// <summary>
/// Global validation pipeline that validates all requests implementing IValidatable.
/// This pipeline executes BEFORE any handler-specific pipelines.
/// </summary>
public class GlobalValidationPipeline
{
    [GlobalPipeline(Order = 10, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
    public async ValueTask<TResponse> ValidateRequest<TRequest, TResponse>(
        PipelineContext<TRequest> ctx,
        PipelineDelegate<TRequest, TResponse> next)
        where TRequest : notnull
        where TResponse : notnull
    {
        Log.Add($"[GlobalValidationPipeline] Validating request type: {typeof(TRequest).Name}");

        // Only validate if request implements IValidatable
        if (ctx.Request is IValidatable validatable)
        {
            var errors = validatable.Validate();
            if (errors.Any())
            {
                var errorMessage = string.Join(", ", errors);
                Log.Add($"[GlobalValidationPipeline] ? Validation failed: {errorMessage}");
                throw new ValidationException(errorMessage);
            }

            Log.Add($"[GlobalValidationPipeline] ? Validation passed");
        }
        else
        {
            Log.Add($"[GlobalValidationPipeline] ? No validation required (not IValidatable)");
        }

        var response = await next(ctx);
        return response;
    }
}

/// <summary>
/// Global exception handler pipeline that catches and logs all exceptions.
/// This pipeline executes as the OUTERMOST layer (Order = 1).
/// </summary>
public class GlobalExceptionHandlerPipeline
{
    [GlobalPipeline(Order = 1, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
    public async ValueTask<TResponse> HandleExceptions<TRequest, TResponse>(
        PipelineContext<TRequest> ctx,
        PipelineDelegate<TRequest, TResponse> next)
        where TRequest : notnull
        where TResponse : notnull
    {
        Log.Add($"[GlobalExceptionHandler] Entering request: {typeof(TRequest).Name}");

        try
        {
            var response = await next(ctx);
            Log.Add($"[GlobalExceptionHandler] Successfully completed: {typeof(TRequest).Name}");
            return response;
        }
        catch (ValidationException ex)
        {
            Log.Add($"[GlobalExceptionHandler] ?? Validation exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Log.Add($"[GlobalExceptionHandler] ?? Unhandled exception in {typeof(TRequest).Name}: {ex.Message}");
            throw;
        }
    }
}
