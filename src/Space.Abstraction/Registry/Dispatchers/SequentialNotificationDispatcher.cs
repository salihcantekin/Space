using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Space.Abstraction.Registry.Dispatchers;

public sealed class SequentialNotificationDispatcher : INotificationDispatcher
{
    public async ValueTask DispatchAsync<TRequest>(IEnumerable<Func<NotificationContext<TRequest>, ValueTask>> handlers, NotificationContext<TRequest> ctx)
    {
        foreach (var handler in handlers)
            await handler(ctx);
    }
}
