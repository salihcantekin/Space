using Microsoft.Extensions.ObjectPool;
using System;
using System.Threading;

namespace Space.Abstraction.Context;

public class NotificationContextPool<TRequest>
{
    private static readonly DefaultObjectPool<NotificationContext<TRequest>> pool =
        new(new NotificationContextPooledObjectPolicy<TRequest>(), maximumRetained: 100);

    public static NotificationContext<TRequest> Get(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        var ctx = pool.Get();
        ctx.Initialize(request, serviceProvider, space, cancellationToken);
        return ctx;
    }

    public static void Return(NotificationContext<TRequest> ctx)
    {
        pool.Return(ctx);
    }
}

public class NotificationContextPooledObjectPolicy<TRequest> : PooledObjectPolicy<NotificationContext<TRequest>>
{
    public override NotificationContext<TRequest> Create() => new();

    public override bool Return(NotificationContext<TRequest> obj) => true;
}
