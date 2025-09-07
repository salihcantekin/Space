using Microsoft.Extensions.ObjectPool;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Space.Abstraction.Context;

public static class HandlerContextPool<TRequest>
{
    private const int MaxRetained = 1024;
    private static readonly DefaultObjectPool<HandlerContext<TRequest>> pool =
        new(new HandlerContextPooledObjectPolicy<TRequest>(), maximumRetained: MaxRetained);

    [ThreadStatic]
    private static HandlerContext<TRequest> threadSlot; // single-thread fast path

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HandlerContext<TRequest> Get(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(HandlerContext<TRequest> ctx)
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

public sealed class HandlerContextPooledObjectPolicy<TRequest> : PooledObjectPolicy<HandlerContext<TRequest>>
{
    public override HandlerContext<TRequest> Create() => new();
    public override bool Return(HandlerContext<TRequest> obj) => true; // always keep
}
