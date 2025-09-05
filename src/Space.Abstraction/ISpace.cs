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

    ValueTask Publish<TRequest>(TRequest request, string name = null, CancellationToken ct = default) where TRequest : notnull;
}
