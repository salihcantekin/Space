using System;
using System.Threading.Tasks;

namespace Space.Abstraction.Modules.Retry;

public sealed class DefaultRetryModuleProvider : IRetryModuleProvider
{
    public async ValueTask<TResponse> Execute<TRequest, TResponse>(PipelineContext<TRequest> ctx, PipelineDelegate<TRequest, TResponse> next, RetryModuleConfig config)
        where TRequest : notnull
        where TResponse : notnull
    {
        Exception last = null;
        var delayMs = config.DelayMilliseconds;

        // RetryCount = number of retries after the initial attempt
        var retries = config.RetryCount < 0 ? 0 : config.RetryCount;

        for (int attempt = 0; attempt <= retries; attempt++)
        {
            try
            {
                // Fresh delegate invocation per attempt; await ValueTask only once per iteration
                var vt = next(ctx);
                if (vt.IsCompletedSuccessfully)
                {
                    return vt.Result;
                }

                var result = await vt;
                return result;
            }
            catch (Exception ex)
            {
                last = ex;
                if (attempt == retries)
                    throw;

                if (delayMs > 0)
                    await Task.Delay(delayMs);
            }
        }

        throw last ?? new InvalidOperationException("Retry failed without exception");
    }
}
