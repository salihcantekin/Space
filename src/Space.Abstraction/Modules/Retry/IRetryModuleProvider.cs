using System.Threading.Tasks;

namespace Space.Abstraction.Modules.Retry;

public interface IRetryModuleProvider : IModuleProvider
{
    ValueTask<TResponse> Execute<TRequest, TResponse>(PipelineContext<TRequest> ctx, PipelineDelegate<TRequest, TResponse> next, RetryModuleConfig config)
        where TRequest : notnull
        where TResponse : notnull;
}
