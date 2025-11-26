using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction;

public interface ISpace
{
    ValueTask Publish<TRequest>(TRequest request, CancellationToken ct = default);
    ValueTask Publish<TRequest>(TRequest request, NotificationDispatchType dispatchType, CancellationToken ct = default);

    // Strongly-typed send for reference-type requests implementing IRequest<TResponse>
    ValueTask<TResponse> Send<TRequest, TResponse>(TRequest request, string name = null, CancellationToken ct = default)
        where TRequest : class, IRequest<TResponse>
        where TResponse : notnull;

    // Struct-friendly send for value-type requests (no IRequest<> requirement)
    ValueTask<TResponse> Send<TRequest, TResponse>(in TRequest request, string name = null, CancellationToken ct = default)
        where TRequest : struct
        where TResponse : notnull;

    // Convenience overload for IRequest<TResponse>
    [EditorBrowsable(EditorBrowsableState.Always)]
    ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, string name = null, CancellationToken ct = default)
        where TResponse : notnull;

    // Object-based send with explicit response type
    [EditorBrowsable(EditorBrowsableState.Always)]
    ValueTask<TResponse> Send<TResponse>(object request, string name = null, CancellationToken ct = default)
        where TResponse : notnull;

    // New: fire-and-forget semantic that still flows through handlers returning Nothing
    [EditorBrowsable(EditorBrowsableState.Always)]
    ValueTask Send(object request, string name = null, CancellationToken ct = default);
}
