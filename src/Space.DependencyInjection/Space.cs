using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction.Extensions;
using System.Runtime.CompilerServices;

namespace Space.DependencyInjection;

public class Space(IServiceProvider serviceProvider) : ISpace
{
    private readonly SpaceRegistry SpaceRegistry = serviceProvider.GetRequiredService<SpaceRegistry>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Send<TRequest, TResponse>(TRequest request, string name = null, CancellationToken ct = default)
        where TRequest : notnull
        where TResponse : notnull
    {
        var ctx = HandlerContext<TRequest>.Create(serviceProvider, request, ct);

        return SpaceRegistry.DispatchHandler<TRequest, TResponse>(ctx, name).AwaitAndReturnHanderInvoke(ctx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<TResponse> Send<TResponse>(object request, string name = null, CancellationToken ct = default)
        where TResponse : notnull
    {
        var result = await SpaceRegistry.DispatchHandler(request, name, ct);

        return (TResponse)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, string name = null, CancellationToken ct = default)
    {
        return Send<TResponse>((object)request, name, ct);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask Publish<TRequest>(TRequest request, string name = null, CancellationToken ct = default)
    {
        var ctx = NotificationContext<TRequest>.Create(serviceProvider, request, ct);

        return SpaceRegistry.DispatchNotification(ctx, name).AwaitAndReturnNotificationInvoke(ctx);
    }

}