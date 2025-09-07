using Microsoft.Extensions.ObjectPool;
using System;
using System.Threading;

namespace Space.Abstraction.Context;

public class NotificationContextPool<TRequest>
{
    private const int MaxRetained = 1024; // standardized with HandlerContextPool
    private static readonly DefaultObjectPool<NotificationContext<TRequest>> pool =
        new(new NotificationContextPooledObjectPolicy<TRequest>(), maximumRetained: MaxRetained);

    [ThreadStatic]
    private static NotificationContext<TRequest> threadSlot; // single-thread fast path

    public static NotificationContext<TRequest> Get(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
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

    public static void Return(NotificationContext<TRequest> ctx)
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

public class NotificationContextPooledObjectPolicy<TRequest> : PooledObjectPolicy<NotificationContext<TRequest>>
{
    public override NotificationContext<TRequest> Create() => new();

    public override bool Return(NotificationContext<TRequest> obj) => true; // always keep
}