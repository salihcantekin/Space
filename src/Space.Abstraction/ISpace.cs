using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction;

public interface ISpace
{
    ValueTask<TResponse> Send<TRequest, TResponse>(TRequest request, string name = null, CancellationToken ct = default)
         where TRequest : notnull
         where TResponse : notnull;

    ValueTask<TResponse> Send<TResponse>(object request, string name = null, CancellationToken ct = default) where TResponse : notnull;

    ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, string name = null, CancellationToken ct = default);

    ValueTask Publish<TRequest>(TRequest request, string name = null, CancellationToken ct = default) where TRequest : notnull;

}
