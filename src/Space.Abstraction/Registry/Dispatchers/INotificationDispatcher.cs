using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Space.Abstraction.Registry.Dispatchers;

public interface INotificationDispatcher
{
    ValueTask DispatchAsync<TRequest>(IEnumerable<Func<NotificationContext<TRequest>, ValueTask>> handlers, NotificationContext<TRequest> ctx);
}
