using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction.Extensions;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Space.DependencyInjection;

public class Space(IServiceProvider rootProvider, IServiceScopeFactory scopeFactory) : ISpace
{
    private readonly SpaceRegistry spaceRegistry = rootProvider.GetRequiredService<SpaceRegistry>();

    private static class EntryCache<TReq, TRes>
    {
        internal static SpaceRegistry.HandlerEntry<TReq, TRes> Entry;
        internal static bool Initialized;
    }

    private static class ObjectEntryCache
    {
        internal static readonly Dictionary<(Type, string), object> Fast = new();
    }

    private static class GenericDispatcherCache<TRes>
    {
        internal static readonly Dictionary<(Type, string), Func<Space, object, CancellationToken, ValueTask<TRes>>> Map = [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsFastPath(ServiceLifetime lifetime) => lifetime == ServiceLifetime.Singleton || lifetime == ServiceLifetime.Transient;

    #region Typed Send

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Send<TRequest, TResponse>(TRequest request, string name = null, CancellationToken ct = default)
        where TRequest : notnull
        where TResponse : notnull
    {
        if (ct.IsCancellationRequested)
            return ValueTask.FromCanceled<TResponse>(ct);

        if (IsFastPath(spaceRegistry.HandlerLifetime))
        {
            var cached = EntryCache<TRequest, TResponse>.Entry;
            if (cached != null)
            {
                if (cached.HasLightInvoker)
                    return cached.InvokeLight(rootProvider, this, request, ct);

                var ctxCached = HandlerContext<TRequest>.Create(rootProvider, request, ct);

                return spaceRegistry.DispatchHandler<TRequest, TResponse>(rootProvider, ctxCached, name).AwaitAndReturnHandlerInvoke(ctxCached);
            }

            if (!EntryCache<TRequest, TResponse>.Initialized && spaceRegistry.TryGetHandlerEntry<TRequest, TResponse>(name, out var first))
            {
                EntryCache<TRequest, TResponse>.Entry = first;
                EntryCache<TRequest, TResponse>.Initialized = true;

                if (first.HasLightInvoker)
                    return first.InvokeLight(rootProvider, this, request, ct);

                var ctxFirst = HandlerContext<TRequest>.Create(rootProvider, request, ct);

                return spaceRegistry.DispatchHandler<TRequest, TResponse>(rootProvider, ctxFirst, name).AwaitAndReturnHandlerInvoke(ctxFirst);
            }

            var ctx = HandlerContext<TRequest>.Create(rootProvider, request, ct);

            return spaceRegistry.DispatchHandler<TRequest, TResponse>(rootProvider, ctx, name).AwaitAndReturnHandlerInvoke(ctx);
        }

        // Scoped path
        var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        if (ct.IsCancellationRequested)
        {
            scope.Dispose();

            return ValueTask.FromCanceled<TResponse>(ct);
        }

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

        var scopedVt = spaceRegistry.DispatchHandler<TRequest, TResponse>(sp, scopedCtx, name).AwaitAndReturnHandlerInvoke(scopedCtx);

        if (scopedVt.IsCompletedSuccessfully)
        {
            scope.Dispose();
            return scopedVt;
        }

        return Await(scopedVt, scope);

        static async ValueTask<TResponse> Await(ValueTask<TResponse> task, IServiceScope scope)
        {
            try
            {
                return await task;
            }
            finally
            {
                scope.Dispose();
            }
        }

        static async ValueTask<TResponse> AwaitLite(ValueTask<TResponse> task, IServiceScope scope)
        {
            try
            {
                return await task;
            }
            finally
            {
                scope.Dispose();
            }
        }
    }

    // Constrained overloads to avoid boxing for IRequest<TResponse>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Send<TRequest, TResponse>(in TRequest request, CancellationToken ct = default)
        where TRequest : struct, IRequest<TResponse>
        where TResponse : notnull
            => Send<TRequest, TResponse>(request, null, ct);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken ct = default)
        where TRequest : class, IRequest<TResponse>
        where TResponse : notnull
            => Send<TRequest, TResponse>(request, null, ct);

    #endregion

    #region Object Send

    // Non-async fast path using generic dispatcher cache -> typed Send to avoid boxing
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Send<TResponse>(object request, string name = null, CancellationToken ct = default)
        where TResponse : notnull
    {
        if (ct.IsCancellationRequested)
            return ValueTask.FromCanceled<TResponse>(ct);

        if (IsFastPath(spaceRegistry.HandlerLifetime))
        {
            var type = request.GetType();
            var key = (type, name ?? string.Empty);

            if (GenericDispatcherCache<TResponse>.Map.TryGetValue(key, out var f))
            {
                return f(this, request, ct);
            }

            // Build and cache dispatcher to typed Send<TRuntime,TResponse>
            var del = BuildTypedDispatcher<TResponse>(type, name);
            GenericDispatcherCache<TResponse>.Map[key] = del;

            return del(this, request, ct);
        }

        return SlowObjectScoped<TResponse>(request, name, ct);

        async ValueTask<TRes> SlowObjectScoped<TRes>(object req, string handlerName, CancellationToken token)
        {
            using var scope = scopeFactory.CreateScope();

            if (token.IsCancellationRequested) 
                return await ValueTask.FromCanceled<TRes>(token);

            var result = await spaceRegistry.DispatchHandler(req, handlerName, typeof(TRes), scope.ServiceProvider, token);

            return (TRes)result!;
        }
    }

    private static Func<Space, object, CancellationToken, ValueTask<TRes>> BuildTypedDispatcher<TRes>(Type requestType, string name)
    {
        // Find generic Send<TRequest, TResponse>(TRequest, string, CancellationToken)
        var mi = typeof(Space).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .First(m => m.Name == nameof(Send) && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 2 &&
                        m.GetParameters() is var ps && ps.Length == 3 && ps[0].ParameterType.IsGenericParameter && ps[1].ParameterType == typeof(string) && ps[2].ParameterType == typeof(CancellationToken));

        var closed = mi.MakeGenericMethod(requestType, typeof(TRes));

        var pThis = Expression.Parameter(typeof(Space), "s");
        var pObj = Expression.Parameter(typeof(object), "o");
        var pCt = Expression.Parameter(typeof(CancellationToken), "ct");
        var nameConst = Expression.Constant(name, typeof(string));

        var call = Expression.Call(pThis, closed, Expression.Convert(pObj, requestType), nameConst, pCt);
        var lambda = Expression.Lambda<Func<Space, object, CancellationToken, ValueTask<TRes>>>(call, pThis, pObj, pCt);

        return lambda.Compile();
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, string name = null, CancellationToken ct = default)
        => Send<TResponse>((object)request, name, ct);

    #region Publish

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask Publish<TRequest>(TRequest request, string name = null, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return ValueTask.FromCanceled(ct);

        if (IsFastPath(spaceRegistry.HandlerLifetime))
        {
            var ctxFast = NotificationContext<TRequest>.Create(rootProvider, request, ct);
            return spaceRegistry.DispatchNotification(ctxFast, name).AwaitAndReturnNotificationInvoke(ctxFast);
        }

        return SlowPublishScoped(request, name, ct);

        async ValueTask SlowPublishScoped(TRequest req, string handlerName, CancellationToken token)
        {
            using var scope = scopeFactory.CreateScope();

            if (token.IsCancellationRequested) 
              return;
            
            var ctx = NotificationContext<TRequest>.Create(scope.ServiceProvider, req, token);
            var vt = spaceRegistry.DispatchNotification(ctx, handlerName).AwaitAndReturnNotificationInvoke(ctx);

            if (!vt.IsCompletedSuccessfully) 
                await vt;
        }
    }

    #endregion
}