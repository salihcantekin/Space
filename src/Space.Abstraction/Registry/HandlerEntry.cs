using Space.Abstraction.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction.Registry;

public delegate ValueTask<TResponse> HandlerInvoker<TRequest, TResponse>(HandlerContext<TRequest> ctx);
public delegate ValueTask<TResponse> PipelineInvoker<TRequest, TResponse>(PipelineContext<TRequest> ctx, PipelineDelegate<TRequest, TResponse> next);

public partial class SpaceRegistry
{
    public interface ILightHandlerEntryInvoker
    {
        ValueTask<TResponse> InvokeLight<TRequest, TResponse>(IServiceProvider sp, ISpace space, TRequest request, CancellationToken ct);
        bool SupportsLight(string name);
    }

    public sealed class HandlerEntry<TRequest, TResponse>(
        HandlerInvoker<TRequest, TResponse> handlerInvoker,
        IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelineInvokers) : IObjectHandlerEntry
    {
        private readonly List<PipelineContainer> pipelines = pipelineInvokers != null
            ? [.. pipelineInvokers.Select(p => new PipelineContainer(p.config, p.invoker))]
            : [];

        private readonly bool hasPipelines = pipelineInvokers != null && pipelineInvokers.Any();

        private readonly object composeLock = new();
        private PipelineContainer[] orderedPipelines; // cached ordered list
        private PipelineDelegate<TRequest, TResponse> cachedRootDelegate; // composed root delegate
        private bool compositionDirty = true;
        private static readonly object HandlerContextItemKey = new();

        // Thread-static reusable context for pipeline-less fast path
        [ThreadStatic]
        private static HandlerContext<TRequest> tsContext;

        private sealed class PipelineContainer(PipelineConfig pipelineConfig, PipelineInvoker<TRequest, TResponse> invoker)
        {
            internal PipelineConfig Config { get; } = pipelineConfig;
            internal PipelineInvoker<TRequest, TResponse> Invoker { get; } = invoker;
        }

        public bool IsPipelineFree => !hasPipelines;

        internal void AddPipeline(PipelineInvoker<TRequest, TResponse> invoker, PipelineConfig pipelineConfig)
        {
            pipelines.Add(new PipelineContainer(pipelineConfig, invoker));
            orderedPipelines = null; // invalidate order cache
            compositionDirty = true; // force recompose
        }

        private PipelineContainer[] GetOrdered()
        {
            if (orderedPipelines != null)
                return orderedPipelines;
            lock (composeLock)
            {
                orderedPipelines ??= pipelines.OrderBy(p => p.Config.Order).ToArray();
            }
            return orderedPipelines;
        }

        private void EnsureComposed()
        {
            if (!compositionDirty)
                return;

            lock (composeLock)
            {
                if (!compositionDirty)
                    return;

                if (pipelines.Count == 0)
                {
                    cachedRootDelegate = null; // no pipeline chain needed
                }
                else
                {
                    var ordered = GetOrdered();

                    // Terminal delegate obtains HandlerContext from Items to stay re-usable across invocations
                    PipelineDelegate<TRequest, TResponse> root = pc =>
                    {
                        var hc = (HandlerContext<TRequest>)pc.GetItem(HandlerContextItemKey);
                        return handlerInvoker(hc);
                    };

                    // Compose in reverse order once
                    for (int i = ordered.Length - 1; i >= 0; i--)
                    {
                        var current = ordered[i];
                        var next = root;
                        root = (ctx) => current.Invoker(ctx, next);
                    }

                    cachedRootDelegate = root;
                }

                compositionDirty = false;
            }
        }

        // Fast light invocation without allocating a context (reuses a thread-static instance)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<TResponse> InvokeLight(IServiceProvider sp, ISpace space, TRequest request, CancellationToken ct)
        {
            var ctx = tsContext;
            if (ctx == null)
            {
                ctx = new HandlerContext<TRequest>();
                tsContext = ctx; // retain for reuse
            }
            ctx.Initialize(request, sp, space, ct);
            return handlerInvoker(ctx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<TResponse> Invoke(HandlerContext<TRequest> handlerContext)
        {
            handlerContext.CancellationToken.ThrowIfCancellationRequested();

            if (!hasPipelines)
            {
                // direct fast path
                return handlerInvoker(handlerContext);
            }

            EnsureComposed();

            var pipelineContext = handlerContext.ToPipelineContext();
            pipelineContext.SetItem(HandlerContextItemKey, handlerContext);

            var vt = cachedRootDelegate(pipelineContext);
            if (vt.IsCompletedSuccessfully)
            {
                // cleanup quickly when completed
                PipelineContextPool<TRequest>.Return(pipelineContext);
                return vt;
            }
            return vt.AwaitAndReturnPipelineInvoke(pipelineContext);
        }

        public ValueTask<object> InvokeObject(HandlerContextStruct handlerContext)
        {
            // Fast path: no pipelines -> use thread-static context reuse
            if (!hasPipelines)
            {
                var ctxFast = tsContext;
                if (ctxFast == null)
                {
                    ctxFast = new HandlerContext<TRequest>();
                    tsContext = ctxFast;
                }
                ctxFast.Initialize((TRequest)handlerContext.Request, handlerContext.ServiceProvider, handlerContext.Space, handlerContext.CancellationToken);
                var vtFast = handlerInvoker(ctxFast);
                if (vtFast.IsCompletedSuccessfully)
                {
                    return new ValueTask<object>(vtFast.Result!);
                }
                return AwaitFast(vtFast);

                static async ValueTask<object> AwaitFast(ValueTask<TResponse> t)
                {
                    var r = await t;
                    return (object)r!;
                }
            }

            // Pipelines path: allocate/pool context normally
            var ctx = HandlerContext<TRequest>.Create(handlerContext);
            var vt = Invoke(ctx);
            if (vt.IsCompletedSuccessfully)
            {
                var result = (object)vt.Result!;
                HandlerContextPool<TRequest>.Return(ctx);
                return new ValueTask<object>(result);
            }
            return Await(vt, ctx);

            static async ValueTask<object> Await(ValueTask<TResponse> t, HandlerContext<TRequest> c)
            {
                try { return (object)await t; }
                finally { HandlerContextPool<TRequest>.Return(c); }
            }
        }
    }
}