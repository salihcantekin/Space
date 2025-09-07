using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction;

public interface ISpace
{
    // Core typed overload
    ValueTask<TResponse> Send<TRequest, TResponse>(TRequest request, string name = null, CancellationToken ct = default)
         where TRequest : notnull
         where TResponse : notnull;

    // Object path kept for dynamic scenarios
    ValueTask<TResponse> Send<TResponse>(object request, string name = null, CancellationToken ct = default) where TResponse : notnull;

    // Notifications: no name parameter
    ValueTask Publish<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : notnull;

    // Per-call dispatcher override using enum
    ValueTask Publish<TRequest>(TRequest request, NotificationDispatchType dispatchType, CancellationToken ct = default) where TRequest : notnull;
}
