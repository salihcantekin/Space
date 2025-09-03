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
    private Dictionary<(Type, string), List<Delegate>> notificationMap = [];
    private Dictionary<Type, List<Delegate>> notificationMapByType = [];
    private ReadOnlyDictionary<(Type, string), List<Delegate>> readOnlyNotificationMap;
    private ReadOnlyDictionary<Type, List<Delegate>> readOnlyNotificationMapByType;
    private bool isSealed = false;
    private readonly INotificationDispatcher notificationDispatcher = notificationDispatcher ?? new SequentialNotificationDispatcher();

    public void RegisterNotification<TRequest>(Func<NotificationContext<TRequest>, ValueTask> handler, string name = "")
    {
        if (handler is null) throw new NotificationHandlerNullException(typeof(TRequest), name);
        if (isSealed)
            throw new NotificationRegistrySealedException();

        var key = SpaceRegistry.GenerateKey<TRequest>(name);
        if (!notificationMap.TryGetValue(key, out var list))
        {
            list = [];
            notificationMap[key] = list;
        }

        list.Add(handler);

        if (!notificationMapByType.TryGetValue(typeof(TRequest), out var typeList))
        {
            typeList = [];
            notificationMapByType[typeof(TRequest)] = typeList;
        }

        typeList.Add(handler);
    }

    public void CompleteRegistration()
    {
        if (isSealed) return;
        readOnlyNotificationMap = new ReadOnlyDictionary<(Type, string), List<Delegate>>(notificationMap);
        readOnlyNotificationMapByType = new ReadOnlyDictionary<Type, List<Delegate>>(notificationMapByType);
        isSealed = true;
        notificationMap = null;
        notificationMapByType = null;
    }

    public ValueTask DispatchNotification<TRequest>(NotificationContext<TRequest> ctx, string name = "")
    {
        if (!isSealed)
            throw new NotificationRegistryNotSealedException();

        var key = SpaceRegistry.GenerateKey<TRequest>(name ?? ctx.HandlerName);

        if (readOnlyNotificationMap!.TryGetValue(key, out var handlers))
        {
            var typedHandlers = handlers.OfType<Func<NotificationContext<TRequest>, ValueTask>>();
            return notificationDispatcher.DispatchAsync(typedHandlers, ctx);
        }

        if (readOnlyNotificationMapByType!.TryGetValue(typeof(TRequest), out var typeHandlers))
        {
            var typedHandlers = typeHandlers.OfType<Func<NotificationContext<TRequest>, ValueTask>>();
            return notificationDispatcher.DispatchAsync(typedHandlers, ctx);
        }

        return default; // nothing to dispatch
    }
}
