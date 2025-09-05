using System;
using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction.Context;

public delegate ValueTask<TRes> PipelineDelegate<TReq, TRes>(PipelineContext<TReq> ctx);

public class PipelineContext<TRequest> : BaseContext<TRequest>, IDisposable
{
    public PipelineContext() : base() { }

    public PipelineContext(TRequest request, IServiceProvider serviceProvider, ISpace Space, CancellationToken cancellationToken)
        : base(request, serviceProvider, Space, cancellationToken)
    {
    }

    public int Order { get; set; } = 100;

    public void Initialize(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        this.Request = request;
        this.CancellationToken = cancellationToken;
        this.ServiceProvider = serviceProvider;
        this.Space = space;

        base.Reset();
    }

    public static PipelineContext<TRequest> Create(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        return PipelineContextPool<TRequest>.Get(request, serviceProvider, space, cancellationToken);
    }

    public void Dispose()
    {
        PipelineContextPool<TRequest>.Return(this);
        GC.SuppressFinalize(this);
    }

    public override string ToString() => $"PipelineContext<{typeof(TRequest).Name}>";
}
