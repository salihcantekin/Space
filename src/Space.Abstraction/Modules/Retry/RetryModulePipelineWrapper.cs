using System;
using System.Threading.Tasks;

namespace Space.Abstraction.Modules.Retry;

public class RetryModulePipelineWrapper<TRequest, TResponse>(RetryModuleConfig cfg, IRetryModuleProvider provider)
    : ModulePipelineWrapper<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    public override ValueTask<TResponse> HandlePipeline(PipelineContext<TRequest> ctx, PipelineDelegate<TRequest, TResponse> next)
    {
        return provider.Execute<TRequest, TResponse>(ctx, next, cfg);
    }
}
