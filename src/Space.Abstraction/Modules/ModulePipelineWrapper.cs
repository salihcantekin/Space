using System.Threading.Tasks;

namespace Space.Abstraction.Modules;

public abstract class ModulePipelineWrapper<TRequest, TResponse> : IPipelineHandler<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    public abstract ValueTask<TResponse> HandlePipeline(PipelineContext<TRequest> ctx, PipelineDelegate<TRequest, TResponse> next);
}
