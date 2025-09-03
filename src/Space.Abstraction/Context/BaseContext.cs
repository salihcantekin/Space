using System;
using System.Collections.Generic;
using System.Threading;



namespace Space.Abstraction.Context
{
    public class BaseContext<TRequest>
    {
        public TRequest Request { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
        public ISpace Space { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public Dictionary<object, object> Items { get; set; } = [];

        public BaseContext() { }
        public BaseContext(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
        {
            Request = request;
            ServiceProvider = serviceProvider;
            Space = space;
            CancellationToken = cancellationToken;
        }
    }

    public static class ContextExtensions
    {
        public static TContext WithRequest<TContext, TRequest>(this TContext context, TRequest request)
            where TContext : BaseContext<TRequest>
        {
            context.Request = request;
            return context;
        }
        public static TContext WithCancellationToken<TContext, TRequest>(this TContext context, CancellationToken cancellationToken)
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
}
