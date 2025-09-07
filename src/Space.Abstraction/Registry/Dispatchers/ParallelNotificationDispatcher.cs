using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Space.Abstraction.Registry.Dispatchers;

public sealed class ParallelNotificationDispatcher : INotificationDispatcher
{
    public async ValueTask DispatchAsync<TRequest>(IEnumerable<Func<NotificationContext<TRequest>, ValueTask>> handlers, NotificationContext<TRequest> ctx)
    {
        var tasks = handlers.Select(handler => handler(ctx).AsTask());
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
