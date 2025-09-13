using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using Space.Abstraction.Contracts;

namespace Space.Abstraction;

public static class ISpaceStrictExtensions
{
    /// <summary>
    /// Preferred strongly-typed Send that enforces TRequest : IRequest&lt;TResponse&gt; at compile time.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public static ValueTask<TResponse> SendStrict<TRequest, TResponse>(this ISpace space, TRequest request, string name = null, CancellationToken ct = default)
        where TRequest : notnull, IRequest<TResponse>
        where TResponse : notnull
        => space.Send<TRequest, TResponse>(request, name, ct);
}
