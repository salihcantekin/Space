using System;
using System.Threading;

namespace Space.Abstraction.Context;

public readonly ref struct LightHandlerContext<TRequest>(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
{
    public TRequest Request { get; } = request;
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public ISpace Space { get; } = space;
    public CancellationToken CancellationToken { get; } = cancellationToken;
}
