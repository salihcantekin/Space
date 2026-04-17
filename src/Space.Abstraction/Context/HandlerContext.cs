using System;
using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction.Context;

public delegate ValueTask<TRes> HandlerDelegate<TReq, TRes>(HandlerContext<TReq> ctx);

public sealed class HandlerContext<TRequest> : BaseContext<TRequest>, IDisposable
{
    public void Initialize(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        Request = request;
        ServiceProvider = serviceProvider;
        Space = space;
        CancellationToken = cancellationToken;
    }

    public static HandlerContext<TRequest> Create(HandlerContextStruct handlerContext)
        => HandlerContextPool<TRequest>.Get((TRequest)handlerContext.Request, handlerContext.ServiceProvider, handlerContext.Space, handlerContext.CancellationToken);

    public static HandlerContext<TRequest> Create(IServiceProvider sp, TRequest request, CancellationToken ct = default) 
        => HandlerContextPool<TRequest>.Get(request, sp, HandlerRegistry.Space, ct);

    public void Dispose()
    {
        HandlerContextPool<TRequest>.Return(this);
    }

    public override string ToString() => $"HandlerContext<{typeof(TRequest).Name}>";
}


public readonly struct HandlerContextStruct(object request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
{
    public object Request { get; } = request;
    public CancellationToken CancellationToken { get; } = cancellationToken;
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public ISpace Space { get; } = space;

    public static HandlerContextStruct Create(IServiceProvider sp, object request, ISpace space, CancellationToken ct = default)
        => new HandlerContextStruct(request, sp, space, ct);
}