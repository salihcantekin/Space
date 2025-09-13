using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction;

public interface ISpace
{
    ValueTask Publish<TRequest>(TRequest request, CancellationToken ct = default);
    ValueTask Publish<TRequest>(TRequest request, NotificationDispatchType dispatchType, CancellationToken ct = default);

    // Preferred strongly-typed send; enforces TRequest : IRequest<TResponse>
    ValueTask<TResponse> Send<TRequest, TResponse>(TRequest request, string name = null, CancellationToken ct = default)
        where TRequest : notnull, IRequest<TResponse>
        where TResponse : notnull;

    // Convenience overload for IRequest<TResponse>
    [EditorBrowsable(EditorBrowsableState.Always)]
    ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, string name = null, CancellationToken ct = default)
        where TResponse : notnull;

    // Object-based send with explicit response type
    [EditorBrowsable(EditorBrowsableState.Always)]
    ValueTask<TResponse> Send<TResponse>(object request, string name = null, CancellationToken ct = default)
        where TResponse : notnull;
}
