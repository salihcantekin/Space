using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction.Extensions;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Space.Abstraction;

namespace Space.DependencyInjection;

public class Space(IServiceProvider rootProvider, IServiceScopeFactory scopeFactory) : ISpace
{
    private readonly SpaceRegistry spaceRegistry = rootProvider.GetRequiredService<SpaceRegistry>();

    private static class EntryCache<TReq, TRes>
    {
        internal static SpaceRegistry.HandlerEntry<TReq, TRes> Entry;          // unnamed fast path
        internal static SpaceRegistry.HandlerEntry<TReq, TRes> NamedEntry;     // last named fast path
        internal static string NamedKey;                                       // last name used
        internal static bool Initialized;
    }

    private static class ObjectEntryCache
    {
        internal static readonly Dictionary<(Type, string), object> Fast = [];
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
            // Unnamed fast path: use Entry cache
            if (string.IsNullOrEmpty(name))
            {
                var cached = EntryCache<TRequest, TResponse>.Entry;
                if (cached != null)
                {
                    if (cached.HasLightInvoker)
                        return cached.InvokeLight(rootProvider, this, request, ct);

                    var ctxCached = HandlerContext<TRequest>.Create(rootProvider, request, ct);
                    return cached.Invoke(ctxCached).AwaitAndReturnHandlerInvoke(ctxCached);
                }

                if (!EntryCache<TRequest, TResponse>.Initialized && spaceRegistry.TryGetHandlerEntry<TRequest, TResponse>(null, out var first))
                {
                    EntryCache<TRequest, TResponse>.Entry = first;
                    EntryCache<TRequest, TResponse>.Initialized = true;

                    if (first.HasLightInvoker)
                        return first.InvokeLight(rootProvider, this, request, ct);

                    var ctxFirst = HandlerContext<TRequest>.Create(rootProvider, request, ct);
                    return first.Invoke(ctxFirst).AwaitAndReturnHandlerInvoke(ctxFirst);
                }
            }
            else
            {
                // Named fast path: remember last used named entry for this TReq/TRes
                if (EntryCache<TRequest, TResponse>.NamedKey == name && EntryCache<TRequest, TResponse>.NamedEntry is { } namedCached)
                {
                    if (namedCached.HasLightInvoker)
                        return namedCached.InvokeLight(rootProvider, this, request, ct);

                    var ctxNamed = HandlerContext<TRequest>.Create(rootProvider, request, ct);
                    return namedCached.Invoke(ctxNamed).AwaitAndReturnHandlerInvoke(ctxNamed);
                }

                if (spaceRegistry.TryGetHandlerEntry<TRequest, TResponse>(name, out var named))
                {
                    EntryCache<TRequest, TResponse>.NamedKey = name;
                    EntryCache<TRequest, TResponse>.NamedEntry = named;

                    if (named.HasLightInvoker)
                        return named.InvokeLight(rootProvider, this, request, ct);

                    var ctxNamed2 = HandlerContext<TRequest>.Create(rootProvider, request, ct);
                    return named.Invoke(ctxNamed2).AwaitAndReturnHandlerInvoke(ctxNamed2);
                }
            }

            // Fallback: legacy registry path
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

        if (spaceRegistry.TryGetHandlerEntry<TRequest, TResponse>(name, out var scopedEntry))
        {
            if (scopedEntry.IsPipelineFree)
            {
                var lite = scopedEntry.InvokeLight(sp, this, request, ct);

                if (lite.IsCompletedSuccessfully)
                {
                    scope.Dispose();
                    return lite;
                }

                return AwaitLite(lite, scope);
            }

            var scopedCtxDirect = HandlerContext<TRequest>.Create(sp, request, ct);
            var scopedVtDirect = scopedEntry.Invoke(scopedCtxDirect).AwaitAndReturnHandlerInvoke(scopedCtxDirect);

            if (scopedVtDirect.IsCompletedSuccessfully)
            {
                scope.Dispose();
                return scopedVtDirect;
            }

            return Await(scopedVtDirect, scope);
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
    public ValueTask Publish<TRequest>(TRequest request, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return ValueTask.FromCanceled(ct);

        if (IsFastPath(spaceRegistry.HandlerLifetime))
        {
            var ctxFast = NotificationContext<TRequest>.Create(rootProvider, request, ct);
            var vt = spaceRegistry.FastDispatchNotification(ctxFast).AwaitAndReturnNotificationInvoke(ctxFast);
            return vt;
        }

        return SlowPublishScoped(request, ct);

        async ValueTask SlowPublishScoped(TRequest req, CancellationToken token)
        {
            using var scope = scopeFactory.CreateScope();

            if (token.IsCancellationRequested)
                return;

            var ctx = NotificationContext<TRequest>.Create(scope.ServiceProvider, req, token);
            var vt = spaceRegistry.DispatchNotification(ctx).AwaitAndReturnNotificationInvoke(ctx);

            if (!vt.IsCompletedSuccessfully)
                await vt;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask Publish<TRequest>(TRequest request, NotificationDispatchType dispatchType, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return ValueTask.FromCanceled(ct);

        if (IsFastPath(spaceRegistry.HandlerLifetime))
        {
            var ctxFast = NotificationContext<TRequest>.Create(rootProvider, request, ct);
            var vt = spaceRegistry.FastDispatchNotification(ctxFast, dispatchType).AwaitAndReturnNotificationInvoke(ctxFast);
            return vt;
        }

        return SlowPublishScoped(request, dispatchType, ct);

        async ValueTask SlowPublishScoped(TRequest req, NotificationDispatchType dt, CancellationToken token)
        {
            using var scope = scopeFactory.CreateScope();

            if (token.IsCancellationRequested)
                return;

            var ctx = NotificationContext<TRequest>.Create(scope.ServiceProvider, req, token);
            var vt = spaceRegistry.DispatchNotification(ctx, dt).AwaitAndReturnNotificationInvoke(ctx);

            if (!vt.IsCompletedSuccessfully)
                await vt;
        }
    }

}