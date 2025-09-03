using Microsoft.Extensions.ObjectPool;
using System;
using System.Threading;

namespace Space.Abstraction.Context;

public class HandlerContextPool<TRequest>
{
    private static readonly DefaultObjectPool<HandlerContext<TRequest>> pool =
        new(new HandlerContextPooledObjectPolicy<TRequest>(), maximumRetained: 100);

    public static HandlerContext<TRequest> Get(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        var ctx = pool.Get();
        ctx.Initialize(request, serviceProvider, space, cancellationToken);
        return ctx;
    }

    public static void Return(HandlerContext<TRequest> ctx)
    {
        pool.Return(ctx);
    }
}

public class HandlerContextPooledObjectPolicy<TRequest> : PooledObjectPolicy<HandlerContext<TRequest>>
{
    public override HandlerContext<TRequest> Create() => new();

    public override bool Return(HandlerContext<TRequest> obj) => true;
}
