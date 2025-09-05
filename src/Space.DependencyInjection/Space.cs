using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction.Extensions;
using System.Runtime.CompilerServices;

namespace Space.DependencyInjection;

public class Space(IServiceProvider rootProvider, IServiceScopeFactory scopeFactory) : ISpace
{
    private readonly SpaceRegistry spaceRegistry = rootProvider.GetRequiredService<SpaceRegistry>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsFastPath(ServiceLifetime lifetime) => lifetime == ServiceLifetime.Singleton || lifetime == ServiceLifetime.Transient;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Send<TRequest, TResponse>(TRequest request, string name = null, CancellationToken ct = default)
        where TRequest : notnull
        where TResponse : notnull
    {
        if (IsFastPath(spaceRegistry.HandlerLifetime))
        {
            // Try lightweight path (pipeline-free)
            if (spaceRegistry.TryGetHandlerEntry<TRequest, TResponse>(name, out var entry) && entry.IsPipelineFree)
            {
                return entry.InvokeLight(rootProvider, this, request, ct);
            }

            var ctx = HandlerContext<TRequest>.Create(rootProvider, request, ct);
            var vt = spaceRegistry.DispatchHandler<TRequest, TResponse>(rootProvider, ctx, name).AwaitAndReturnHanderInvoke(ctx);
            return vt;
        }

        var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        if (spaceRegistry.TryGetHandlerEntry<TRequest, TResponse>(name, out var scopedEntry) && scopedEntry.IsPipelineFree)
        {
            var lite = scopedEntry.InvokeLight(sp, this, request, ct);
            if (lite.IsCompletedSuccessfully)
            {
                scope.Dispose();
                return lite;
            }
            return AwaitLite(lite, scope);
        }

        var scopedCtx = HandlerContext<TRequest>.Create(sp, request, ct);
        var scopedVt = spaceRegistry.DispatchHandler<TRequest, TResponse>(sp, scopedCtx, name).AwaitAndReturnHanderInvoke(scopedCtx);
        if (scopedVt.IsCompletedSuccessfully)
        {
            scope.Dispose();
            return scopedVt;
        }

        return Await(scopedVt, scope);

        static async ValueTask<TResponse> Await(ValueTask<TResponse> task, IServiceScope scope)
        {
            try { return await task; } finally { scope.Dispose(); }
        }
        static async ValueTask<TResponse> AwaitLite(ValueTask<TResponse> task, IServiceScope scope)
        {
            try { return await task; } finally { scope.Dispose(); }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<TResponse> Send<TResponse>(object request, string name = null, CancellationToken ct = default)
        where TResponse : notnull
    {
        if (IsFastPath(spaceRegistry.HandlerLifetime))
        {
            // object path currently falls back to regular dispatch
            var resultFast = await spaceRegistry.DispatchHandler(request, name, rootProvider, ct);
            return (TResponse)resultFast;
        }

        using var scope = scopeFactory.CreateScope();
        var result = await spaceRegistry.DispatchHandler(request, name, scope.ServiceProvider, ct);
        return (TResponse)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, string name = null, CancellationToken ct = default)
    {
        return Send<TResponse>((object)request, name, ct);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask Publish<TRequest>(TRequest request, string name = null, CancellationToken ct = default)
    {
        if (IsFastPath(spaceRegistry.HandlerLifetime))
        {
            var ctxFast = NotificationContext<TRequest>.Create(rootProvider, request, ct);
            return spaceRegistry.DispatchNotification(ctxFast, name).AwaitAndReturnNotificationInvoke(ctxFast);
        }

        var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var ctx = NotificationContext<TRequest>.Create(sp, request, ct);
        var vt = spaceRegistry.DispatchNotification(ctx, name).AwaitAndReturnNotificationInvoke(ctx);
        if (vt.IsCompletedSuccessfully)
        {
            scope.Dispose();
            return vt;
        }
        return Await(vt, scope);

        static async ValueTask Await(ValueTask task, IServiceScope scope)
        {
            try { await task; } finally { scope.Dispose(); }
        }
    }
}