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
            // Fast path: avoid LINQ allocations when using sequential or parallel dispatcher
            if (defaultDispatcher is SequentialNotificationDispatcher)
            {
                return DispatchSequentialInline(handlers, ctx);
            }
            if (defaultDispatcher is ParallelNotificationDispatcher)
            {
                return DispatchParallelInline(handlers, ctx);
            }

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
            if (dispatcher is SequentialNotificationDispatcher)
            {
                return DispatchSequentialInline(handlers, ctx);
            }
            if (dispatcher is ParallelNotificationDispatcher)
            {
                return DispatchParallelInline(handlers, ctx);
            }

            var typedHandlers = handlers.OfType<Func<NotificationContext<TRequest>, ValueTask>>();
            return dispatcher.DispatchAsync(typedHandlers, ctx);
        }

        return default;
    }

    private static ValueTask DispatchSequentialInline<TRequest>(List<Delegate> handlers, NotificationContext<TRequest> ctx)
    {
        // Iterate and invoke without LINQ to avoid iterator allocations. Use a single async state machine only if needed.
        for (int i = 0; i < handlers.Count; i++)
        {
            var h = (Func<NotificationContext<TRequest>, ValueTask>)handlers[i];
            var vt = h(ctx);

            if (!vt.IsCompletedSuccessfully)
            {
                return Await(handlers, ctx, i, vt);
            }
        }

        return default;

        static async ValueTask Await(List<Delegate> hs, NotificationContext<TRequest> c, int startIndex, ValueTask pending)
        {
            // Await the first pending one
            await pending.ConfigureAwait(false);

            // Continue awaiting the rest (if any)
            for (int j = startIndex + 1; j < hs.Count; j++)
            {
                var h2 = (Func<NotificationContext<TRequest>, ValueTask>)hs[j];
                var vt2 = h2(c);

                if (!vt2.IsCompletedSuccessfully)
                    await vt2.ConfigureAwait(false);
            }
        }
    }

    private static ValueTask DispatchParallelInline<TRequest>(List<Delegate> handlers, NotificationContext<TRequest> ctx)
    {
        // Fire all handlers first (ensures parallelism), collect incomplete ValueTasks and await them with a single state machine.
        List<ValueTask> pending = null;
        for (int i = 0; i < handlers.Count; i++)
        {
            var h = (Func<NotificationContext<TRequest>, ValueTask>)handlers[i];
            var vt = h(ctx);

            if (!vt.IsCompletedSuccessfully)
            {
                (pending ??= new List<ValueTask>(handlers.Count - i)).Add(vt);
            }
        }

        if (pending is null || pending.Count == 0)
            return default;

        return AwaitAll(pending);

        static async ValueTask AwaitAll(List<ValueTask> list)
        {
            // Await each pending task; they are already running, so this preserves parallelism without Task.WhenAll allocations.
            for (int i = 0; i < list.Count; i++)
            {
                var vt = list[i];

                if (!vt.IsCompletedSuccessfully)
                    await vt.ConfigureAwait(false);
            }
        }
    }
}
