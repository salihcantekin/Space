using System.Threading.Tasks;

namespace Space.Abstraction.Contracts;

/// <summary>
/// Optional marker interface for global pipeline handlers.
/// Provides compile-time type safety for the HandleGlobalPipeline method signature.
/// The GlobalPipelineAttribute is required for discovery; this interface is optional but recommended.
/// </summary>
/// <typeparam name="TRequest">The request type this global pipeline handles</typeparam>
/// <typeparam name="TResponse">The response type this global pipeline handles</typeparam>
public interface IGlobalPipeline<TRequest, TResponse>
{
    /// <summary>
    /// Handles the global pipeline execution for all handlers with matching request and response types.
    /// </summary>
    /// <param name="ctx">The pipeline context containing the request and services</param>
    /// <param name="next">The next pipeline in the chain (including handler-specific pipelines and the handler itself)</param>
    /// <returns>The response from the pipeline chain</returns>
    ValueTask<TResponse> HandleGlobalPipeline(PipelineContext<TRequest> ctx, PipelineDelegate<TRequest, TResponse> next);
}
