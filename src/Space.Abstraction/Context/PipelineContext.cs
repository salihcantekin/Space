using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Space.Abstraction.Context;

public delegate ValueTask<TRes> PipelineDelegate<TReq, TRes>(PipelineContext<TReq> ctx);

public sealed class PipelineContext<TRequest> : BaseContext<TRequest>, IDisposable
{
    public PipelineContext() : base() { }

    public PipelineContext(TRequest request, IServiceProvider serviceProvider, ISpace Space, CancellationToken cancellationToken)
        : base(request, serviceProvider, Space, cancellationToken)
    {
    }

    public int Order { get; set; } = 100;

    // Strongly-typed per-invocation carrier to avoid dictionary lookups when calling the final handler from pipeline chain.
    internal HandlerContext<TRequest> HandlerContextRef;

    // Items are pipeline-specific
    private ItemsHolder itemsHolder;

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

            if (Key != null && Equals(Key, key))
                return Value;

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
            {
                Key = key; Value = value;
                return;
            }

            // second item -> upgrade
            Dict = new Dictionary<object, object>(2)
            {
                { Key, Value },
                { key, value }
            };

            Key = null;
            Value = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            Key = null;
            Value = null;
            Dict?.Clear();
            Dict = null;
        }
    }

    public object GetItem(object key) => itemsHolder?.Get(key);

    public void SetItem(object key, object value)
    {
        (itemsHolder ??= new ItemsHolder()).Set(key, value);
    }

    internal void ClearItems() => itemsHolder?.Clear();

    public void Initialize(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        this.Request = request;
        this.CancellationToken = cancellationToken;
        this.ServiceProvider = serviceProvider;
        this.Space = space;

        // Reset per-invocation holder
        HandlerContextRef = null;
        ClearItems();
    }

    public static PipelineContext<TRequest> Create(TRequest request, IServiceProvider serviceProvider, ISpace space, CancellationToken cancellationToken)
    {
        return PipelineContextPool<TRequest>.Get(request, serviceProvider, space, cancellationToken);
    }

    public void Dispose()
    {
        PipelineContextPool<TRequest>.Return(this);
        GC.SuppressFinalize(this);
    }

    public override string ToString() => $"PipelineContext<{typeof(TRequest).Name}>";
}
