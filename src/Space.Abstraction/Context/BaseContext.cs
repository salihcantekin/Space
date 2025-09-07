using System;
using System.Threading;

namespace Space.Abstraction.Context;

public class BaseContext<TRequest>
{
    public TRequest Request { get; set; }
    public IServiceProvider ServiceProvider { get; set; }
    public ISpace Space { get; set; }
    public CancellationToken CancellationToken { get; set; }

    public BaseContext() { }

    public BaseContext(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        Request = request;
        ServiceProvider = serviceProvider;
        Space = space;
        CancellationToken = cancellationToken;
    }
}
