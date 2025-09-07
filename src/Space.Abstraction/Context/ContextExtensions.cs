using System.Threading;

namespace Space.Abstraction.Context;

internal static class ContextExtensions
{
    internal static TContext WithRequest<TContext, TRequest>(this TContext context, TRequest request)
        where TContext : BaseContext<TRequest>
    {
        context.Request = request;
        return context;
    }

    internal static TContext WithCancellationToken<TContext, TRequest>(this TContext context, CancellationToken cancellationToken)
        where TContext : BaseContext<TRequest>
    {
        context.CancellationToken = cancellationToken;
        return context;
    }

    public static PipelineContext<TRequest> ToPipelineContext<TRequest>(this HandlerContext<TRequest> handlerContext)
    {
        return PipelineContext<TRequest>.Create(handlerContext.Request,
                                                handlerContext.ServiceProvider,
                                                handlerContext.Space,
                                                handlerContext.CancellationToken);
    }
}
