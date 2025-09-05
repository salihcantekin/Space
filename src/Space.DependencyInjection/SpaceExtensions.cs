using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Space.Abstraction;
using Space.Abstraction.Contracts;

namespace Space.DependencyInjection;

public static class SpaceExtensions
{
    // For struct requests implementing IRequest<TResponse>. Avoids boxing and allows type inference when the result is target-typed
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<TResponse> Send<TRequest, TResponse>(this ISpace space, in TRequest request, CancellationToken ct = default)
        where TRequest : struct, IRequest<TResponse>
        where TResponse : notnull
        => space.Send<TRequest, TResponse>(request, null, ct);

    // For class requests implementing IRequest<TResponse>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<TResponse> Send<TRequest, TResponse>(this ISpace space, TRequest request, CancellationToken ct = default)
        where TRequest : class, IRequest<TResponse>
        where TResponse : notnull
        => space.Send<TRequest, TResponse>(request, null, ct);
}
