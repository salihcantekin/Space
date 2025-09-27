// Shared helper methods used by generated Space source generator output.
// Placed in Abstraction so both runtime and generated code can reuse logic
// without duplicating boilerplate inside templates.
using System;
using System.Threading.Tasks;
using Space.Abstraction.Context;
using Space.Abstraction.Registry; // for delegate types

namespace Space.Abstraction.Helpers;

public static class SpaceGeneratorRuntimeHelpers
{
    // Fast Task<T> -> ValueTask<T> adaptation
    public static ValueTask<T> VT<T>(Task<T> task)
        => task.Status == TaskStatus.RanToCompletion ? new ValueTask<T>(task.Result) : new ValueTask<T>(task);

    // Thread-static lightweight HandlerContext reuse for light handlers.
    internal static class LightInvokerState<TReq, THandler, TRes>
    {
        [ThreadStatic] private static HandlerContext<TReq> _ctx;
        public static ValueTask<TRes> Invoke(in LightHandlerContext<TReq> lctx, THandler singleton, bool isSingleton,
            Func<THandler, HandlerContext<TReq>, ValueTask<TRes>> body)
        {
            var ctx = _ctx;
            if (ctx == null)
            {
                ctx = new HandlerContext<TReq>();
                _ctx = ctx;
            }
            ctx.Initialize(lctx.Request, lctx.ServiceProvider, lctx.Space, lctx.CancellationToken);
            var inst = isSingleton ? singleton : GetRequired<THandler>(ctx.ServiceProvider);
            return body(inst, ctx);
        }
    }

    public static LightHandlerInvoker<TReq, TRes> CreateLightInvoker<TReq, THandler, TRes>(
        THandler singleton, bool isSingleton,
        Func<THandler, HandlerContext<TReq>, ValueTask<TRes>> body)
    {
        return (in LightHandlerContext<TReq> lctx) => LightInvokerState<TReq, THandler, TRes>.Invoke(in lctx, singleton, isSingleton, body);
    }

    private static T GetRequired<T>(IServiceProvider sp)
    {
        var obj = sp.GetService(typeof(T));
        if (obj is null)
            throw new InvalidOperationException($"Required service of type {typeof(T)} not registered.");
        return (T)obj;
    }
}
