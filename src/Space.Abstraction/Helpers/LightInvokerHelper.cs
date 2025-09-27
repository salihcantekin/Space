using Space.Abstraction.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Space.DependencyInjection;

// Runtime light-handler fast path helper (moved out of generator)
public static class LightInvokerHelper
{
    public static ValueTask<TRes> Invoke<TReq, THandler, TRes>(
        in LightHandlerContext<TReq> lctx,
        THandler singletonInstance,
        bool isSingleton,
        Func<THandler, HandlerContext<TReq>, ValueTask<TRes>> body)
    {
        // Resolve handler
        var handler = isSingleton
            ? singletonInstance
            : (THandler)lctx.ServiceProvider.GetService(typeof(THandler));

        var ctx = HandlerContext<TReq>.Create(lctx.ServiceProvider, lctx.Request, lctx.CancellationToken);
        var vt = body(handler, ctx);

        if (vt.IsCompletedSuccessfully)
        {
            HandlerContextPool<TReq>.Return(ctx);
            return vt;
        }

        return vt.AwaitAndReturnHandlerInvoke(ctx);
    }
}
