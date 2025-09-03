using Microsoft.Extensions.ObjectPool;
using System;
using System.Threading;

namespace Space.Abstraction.Context;

public class PipelineContextPool<TRequest>
{
    private static readonly DefaultObjectPool<PipelineContext<TRequest>> pool =
        new(new PipelineContextPooledObjectPolicy<TRequest>());

    public static PipelineContext<TRequest> Get(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        var ctx = pool.Get();
        ctx.Initialize(request, serviceProvider, space, cancellationToken);
        return ctx;
    }

    public static void Return(PipelineContext<TRequest> ctx)
    {
        pool.Return(ctx);
    }
}

public class PipelineContextPooledObjectPolicy<TRequest> : PooledObjectPolicy<PipelineContext<TRequest>>
{
    public override PipelineContext<TRequest> Create() => new();

    public override bool Return(PipelineContext<TRequest> obj) => true;
}
