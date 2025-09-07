using Microsoft.Extensions.ObjectPool;
using System;
using System.Threading;

namespace Space.Abstraction.Context;

public class PipelineContextPool<TRequest>
{
    private const int MaxRetained = 1024; // standardized with HandlerContextPool
    private static readonly DefaultObjectPool<PipelineContext<TRequest>> pool =
        new(new PipelineContextPooledObjectPolicy<TRequest>(), maximumRetained: MaxRetained);

    [ThreadStatic]
    private static PipelineContext<TRequest> threadSlot; // single-thread fast path

    public static PipelineContext<TRequest> Get(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        // Fast thread-local reuse (avoids pool hit & lock-free path inside pool)
        var ctx = threadSlot;
        if (ctx != null)
        {
            threadSlot = null; // consume
        }
        else
        {
            ctx = pool.Get();
        }
        ctx.Initialize(request, serviceProvider, space, cancellationToken);
        return ctx;
    }

    public static void Return(PipelineContext<TRequest> ctx)
    {
        // Try place back into thread slot, otherwise pool
        if (threadSlot == null)
        {
            // Clear per-request state already done in Initialize on next use
            threadSlot = ctx;
            return;
        }
        pool.Return(ctx);
    }
}

public class PipelineContextPooledObjectPolicy<TRequest> : PooledObjectPolicy<PipelineContext<TRequest>>
{
    public override PipelineContext<TRequest> Create() => new();

    public override bool Return(PipelineContext<TRequest> obj) => true; // always keep
}