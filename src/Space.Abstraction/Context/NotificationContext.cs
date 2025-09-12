using System;
using System.Threading;

namespace Space.Abstraction.Context;

public sealed class NotificationContext<TRequest> : BaseContext<TRequest>, IDisposable
{
    public void Initialize(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        Request = request;
        ServiceProvider = serviceProvider;
        Space = space;
        CancellationToken = cancellationToken;
    }

    public static NotificationContext<TRequest> Create(HandlerContextStruct handlerContext)
    {
        return NotificationContextPool<TRequest>.Get((TRequest)handlerContext.Request, handlerContext.ServiceProvider, handlerContext.Space, handlerContext.CancellationToken);
    }

    public static NotificationContext<TRequest> Create(IServiceProvider sp, TRequest request, CancellationToken ct = default)
    {
        return NotificationContextPool<TRequest>.Get(request, sp, HandlerRegistry.Space, ct);
    }

    public void Dispose()
    {
        NotificationContextPool<TRequest>.Return(this);
        GC.SuppressFinalize(this);
    }

    public override string ToString() => $"NotificationContext<{typeof(TRequest).Name}>";
}