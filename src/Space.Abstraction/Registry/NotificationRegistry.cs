using Space.Abstraction.Registry.Dispatchers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Space.Abstraction.Exceptions;

namespace Space.Abstraction.Registry;

public class NotificationRegistry(INotificationDispatcher notificationDispatcher)
{
    private Dictionary<Type, List<Delegate>> notificationMapByType = [];
    private ReadOnlyDictionary<Type, List<Delegate>> readOnlyNotificationMapByType;
    private bool isSealed = false;

    private readonly INotificationDispatcher defaultDispatcher = notificationDispatcher ?? new SequentialNotificationDispatcher();
    private readonly INotificationDispatcher sequentialDispatcher = notificationDispatcher is SequentialNotificationDispatcher seq ? seq : new SequentialNotificationDispatcher();
    private readonly INotificationDispatcher parallelDispatcher = notificationDispatcher is ParallelNotificationDispatcher par ? par : new ParallelNotificationDispatcher();

    public void RegisterNotification<TRequest>(Func<NotificationContext<TRequest>, ValueTask> handler)
    {
        if (handler is null)
            throw new NotificationHandlerNullException(typeof(TRequest), string.Empty);

        if (isSealed)
            throw new NotificationRegistrySealedException();

        if (!notificationMapByType.TryGetValue(typeof(TRequest), out var list))
        {
            list = [];
            notificationMapByType[typeof(TRequest)] = list;
        }

        list.Add(handler);
    }

    public void CompleteRegistration()
    {
        if (isSealed) return;

        readOnlyNotificationMapByType = new ReadOnlyDictionary<Type, List<Delegate>>(notificationMapByType);
        isSealed = true;
        notificationMapByType = null;
    }

    public ValueTask DispatchNotification<TRequest>(NotificationContext<TRequest> ctx)
    {
        if (!isSealed)
            throw new NotificationRegistryNotSealedException();

        if (readOnlyNotificationMapByType!.TryGetValue(typeof(TRequest), out var handlers))
        {
            var typedHandlers = handlers.OfType<Func<NotificationContext<TRequest>, ValueTask>>();
            return defaultDispatcher.DispatchAsync(typedHandlers, ctx);
        }

        return default;
    }

    public ValueTask DispatchNotification<TRequest>(NotificationContext<TRequest> ctx, NotificationDispatchType dispatchType)
    {
        if (!isSealed)
            throw new NotificationRegistryNotSealedException();

        var dispatcher = dispatchType == NotificationDispatchType.Parallel ? parallelDispatcher : sequentialDispatcher;

        if (readOnlyNotificationMapByType!.TryGetValue(typeof(TRequest), out var handlers))
        {
            var typedHandlers = handlers.OfType<Func<NotificationContext<TRequest>, ValueTask>>();
            return dispatcher.DispatchAsync(typedHandlers, ctx);
        }

        return default;
    }
}
