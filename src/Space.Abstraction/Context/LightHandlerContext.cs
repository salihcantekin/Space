using System;
using System.Threading;

namespace Space.Abstraction.Context;

public readonly ref struct LightHandlerContext<TRequest>
{
    public TRequest Request { get; }
    public IServiceProvider ServiceProvider { get; }
    public ISpace Space { get; }
    public CancellationToken CancellationToken { get; }

    public LightHandlerContext(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        Request = request;
        ServiceProvider = serviceProvider;
        Space = space;
        CancellationToken = cancellationToken;
    }
}
