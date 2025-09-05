using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Space.Abstraction.Context;

public class BaseContext<TRequest>
{
    public TRequest Request { get; set; }
    public IServiceProvider ServiceProvider { get; set; }
    public ISpace Space { get; set; }
    public CancellationToken CancellationToken { get; set; }

    // Lazy items holder (single item optimized -> dictionary only when needed)
    private ItemsHolder _itemsHolder; // null until first SetItem

    private sealed class ItemsHolder
    {
        internal object Key;
        internal object Value;
        internal Dictionary<object, object> Dict; // created on second add
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal object Get(object key)
        {
            if (Dict != null)
                return Dict.TryGetValue(key, out var v) ? v : null;
            if (Key != null && Equals(Key, key)) return Value;
            return null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Set(object key, object value)
        {
            if (Dict != null)
            {
                Dict[key] = value; return;
            }
            if (Key == null)
            { Key = key; Value = value; return; }
            // second item -> upgrade
            Dict = new Dictionary<object, object>(2)
            {
                { Key, Value },
                { key, value }
            };
            Key = null; Value = null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            Key = null; Value = null; Dict?.Clear(); Dict = null;
        }
    }

    public object GetItem(object key) => _itemsHolder?.Get(key);

    public void SetItem(object key, object value)
    {
        (_itemsHolder ??= new ItemsHolder()).Set(key, value);
    }

    public void ClearItems() => _itemsHolder?.Clear();

    public BaseContext() { }
    public BaseContext(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        Request = request;
        ServiceProvider = serviceProvider;
        Space = space;
        CancellationToken = cancellationToken;
    }

    public void Reset()
    {
        ClearItems();
    }
}

public static class ContextExtensions
{
    public static TContext WithRequest<TContext, TRequest>(this TContext context, TRequest request)
        where TContext : BaseContext<TRequest>
    {
        context.Request = request;
        return context;
    }
    public static TContext WithCancellationToken<TContext, TRequest>(this TContext context, CancellationToken cancellationToken)
        where TContext : BaseContext<TRequest>
    {
        context.CancellationToken = cancellationToken;
        return context;
    }

    public static PipelineContext<TRequest> ToPipelineContext<TRequest>(this HandlerContext<TRequest> handlerContext)
    {
        return PipelineContext<TRequest>.Create(handlerContext.Request,
                                                handlerContext.ServiceProvider,
                                                handlerContext.Space,
                                                handlerContext.CancellationToken);
    }
}
