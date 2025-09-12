using Space.Abstraction.Registry.Dispatchers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Space.Abstraction.Exceptions;

namespace Space.Abstraction.Registry;

public class NotificationRegistry(INotificationDispatcher notificationDispatcher)
{
    private Dictionary<Type, List<Delegate>> notificationMapByType = [];
    private ReadOnlyDictionary<Type, object> typedHandlers; // Func<NotificationContext<T>, ValueTask>[] boxed as object
    private bool isSealed = false;

    private readonly INotificationDispatcher defaultDispatcher = notificationDispatcher ?? new SequentialNotificationDispatcher();
    private readonly INotificationDispatcher sequentialDispatcher = notificationDispatcher is SequentialNotificationDispatcher seq ? seq : new SequentialNotificationDispatcher();
    private readonly INotificationDispatcher parallelDispatcher = notificationDispatcher is ParallelNotificationDispatcher par ? par : new ParallelNotificationDispatcher();

    private readonly bool defaultIsSequential = notificationDispatcher is SequentialNotificationDispatcher || notificationDispatcher is null;
    private readonly bool defaultIsParallel = notificationDispatcher is ParallelNotificationDispatcher;

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

        var typed = new Dictionary<Type, object>(notificationMapByType.Count);
        foreach (var kv in notificationMapByType)
        {
            var type = kv.Key;
            var list = kv.Value;
            var method = typeof(NotificationRegistry).GetMethod(nameof(BuildTypedArray), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var mm = method.MakeGenericMethod(type);
            var arr = mm.Invoke(null, new object[] { list });
            typed[type] = arr!;
        }
        typedHandlers = new ReadOnlyDictionary<Type, object>(typed);

        isSealed = true;
        notificationMapByType = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ValueTask FastDispatch<TRequest>(NotificationContext<TRequest> ctx)
    {
        if (!isSealed)
            return default;

        if (typedHandlers != null && typedHandlers.TryGetValue(typeof(TRequest), out var obj))
        {
            var handlers = (Func<NotificationContext<TRequest>, ValueTask>[])obj;

            if (defaultIsSequential)
                return DispatchSequential(handlers, ctx);
            if (defaultIsParallel)
                return DispatchParallel(handlers, ctx);

            return defaultDispatcher.DispatchAsync(handlers, ctx);
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ValueTask FastDispatch<TRequest>(NotificationContext<TRequest> ctx, NotificationDispatchType dispatchType)
    {
        if (!isSealed)
            return default;

        if (typedHandlers != null && typedHandlers.TryGetValue(typeof(TRequest), out var obj))
        {
            var handlers = (Func<NotificationContext<TRequest>, ValueTask>[])obj;

            return dispatchType == NotificationDispatchType.Sequential
                ? DispatchSequential(handlers, ctx)
                : DispatchParallel(handlers, ctx);
        }

        return default;
    }

    public ValueTask DispatchNotification<TRequest>(NotificationContext<TRequest> ctx)
    {
        if (!isSealed)
            throw new NotificationRegistryNotSealedException();

        if (typedHandlers != null && typedHandlers.TryGetValue(typeof(TRequest), out var obj))
        {
            var handlers = (Func<NotificationContext<TRequest>, ValueTask>[])obj;

            if (defaultIsSequential)
                return DispatchSequential(handlers, ctx);
            if (defaultIsParallel)
                return DispatchParallel(handlers, ctx);

            return defaultDispatcher.DispatchAsync(handlers, ctx);
        }

        return default;
    }

    public ValueTask DispatchNotification<TRequest>(NotificationContext<TRequest> ctx, NotificationDispatchType dispatchType)
    {
        if (!isSealed)
            throw new NotificationRegistryNotSealedException();

        if (typedHandlers != null && typedHandlers.TryGetValue(typeof(TRequest), out var obj))
        {
            var handlers = (Func<NotificationContext<TRequest>, ValueTask>[])obj;

            return dispatchType == NotificationDispatchType.Sequential
                ? DispatchSequential(handlers, ctx)
                : DispatchParallel(handlers, ctx);
        }

        return default;
    }

    private static Func<NotificationContext<TRequest>, ValueTask>[] BuildTypedArray<TRequest>(List<Delegate> handlers)
    {
        if (handlers is null || handlers.Count == 0)
            return Array.Empty<Func<NotificationContext<TRequest>, ValueTask>>();

        var arr = new Func<NotificationContext<TRequest>, ValueTask>[handlers.Count];
        for (int i = 0; i < handlers.Count; i++)
            arr[i] = (Func<NotificationContext<TRequest>, ValueTask>)handlers[i];
        return arr;
    }

    private static ValueTask DispatchSequential<TRequest>(Func<NotificationContext<TRequest>, ValueTask>[] handlers, NotificationContext<TRequest> ctx)
    {
        for (int i = 0; i < handlers.Length; i++)
        {
            var vt = handlers[i](ctx);
            if (!vt.IsCompletedSuccessfully)
            {
                return Await(handlers, ctx, i, vt);
            }
        }
        return default;

        static async ValueTask Await(Func<NotificationContext<TRequest>, ValueTask>[] hs, NotificationContext<TRequest> c, int startIndex, ValueTask pending)
        {
            await pending.ConfigureAwait(false);
            for (int j = startIndex + 1; j < hs.Length; j++)
            {
                var vt2 = hs[j](c);
                if (!vt2.IsCompletedSuccessfully)
                    await vt2.ConfigureAwait(false);
            }
        }
    }

    private static ValueTask DispatchParallel<TRequest>(Func<NotificationContext<TRequest>, ValueTask>[] handlers, NotificationContext<TRequest> ctx)
    {
        List<ValueTask> pending = null;
        for (int i = 0; i < handlers.Length; i++)
        {
            var vt = handlers[i](ctx);
            if (!vt.IsCompletedSuccessfully)
            {
                (pending ??= new List<ValueTask>(handlers.Length - i)).Add(vt);
            }
        }

        if (pending is null || pending.Count == 0)
            return default;

        return AwaitAll(pending);

        static async ValueTask AwaitAll(List<ValueTask> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var vt = list[i];
                if (!vt.IsCompletedSuccessfully)
                    await vt.ConfigureAwait(false);
            }
        }
    }
}
