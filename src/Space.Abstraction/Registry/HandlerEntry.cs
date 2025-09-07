using Space.Abstraction.Extensions;
using Space.Abstraction.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction.Registry;

public delegate ValueTask<TResponse> HandlerInvoker<TRequest, TResponse>(HandlerContext<TRequest> ctx);
public delegate ValueTask<TResponse> LightHandlerInvoker<TRequest, TResponse>(in LightHandlerContext<TRequest> lctx);
public delegate ValueTask<TResponse> PipelineInvoker<TRequest, TResponse>(PipelineContext<TRequest> ctx, PipelineDelegate<TRequest, TResponse> next);

public partial class SpaceRegistry
{
    internal sealed class HandlerEntry<TRequest, TResponse> : IObjectHandlerEntry
    {
        private readonly HandlerInvoker<TRequest, TResponse> handlerInvoker;
        private readonly LightHandlerInvoker<TRequest, TResponse> lightInvoker;
        private readonly List<PipelineContainer> pipelines;
        private bool hasPipelines; // must be mutable to reflect late pipeline registration

        private readonly object composeLock = new();
        private PipelineContainer[] orderedPipelines; // cached ordered list
        private PipelineDelegate<TRequest, TResponse> cachedRootDelegate; // composed root delegate
        private bool compositionDirty = true;
        private static readonly object HandlerContextItemKey = new();

        private sealed class PipelineContainer
        {
            internal PipelineConfig Config { get; }
            internal PipelineInvoker<TRequest, TResponse> Invoker { get; }
            internal PipelineContainer(PipelineConfig pipelineConfig, PipelineInvoker<TRequest, TResponse> invoker)
            { Config = pipelineConfig; Invoker = invoker; }
        }

        internal bool IsPipelineFree => !hasPipelines;
        internal bool HasLightInvoker => lightInvoker != null && !hasPipelines;

        internal HandlerEntry(
            HandlerInvoker<TRequest, TResponse> handlerInvoker,
            LightHandlerInvoker<TRequest, TResponse> lightInvoker,
            IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelineInvokers)
        {
            this.handlerInvoker = handlerInvoker;
            this.lightInvoker = lightInvoker;
            pipelines = pipelineInvokers != null
                ? new List<PipelineContainer>(pipelineInvokers.Select(p => new PipelineContainer(p.config, p.invoker)))
                : new List<PipelineContainer>();
            hasPipelines = pipelines.Count > 0;
        }

        internal void AddPipeline(PipelineInvoker<TRequest, TResponse> invoker, PipelineConfig pipelineConfig)
        {
            pipelines.Add(new PipelineContainer(pipelineConfig, invoker));
            hasPipelines = true; // ensure fast-path bypasses light invoker when any pipeline exists
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

                if (!hasPipelines || pipelines.Count == 0)
                {
                    cachedRootDelegate = null; // no pipeline chain needed
                }
                else
                {
                    var ordered = GetOrdered();

                    PipelineDelegate<TRequest, TResponse> root = pc =>
                    {
                        var hc = (HandlerContext<TRequest>)pc.GetItem(HandlerContextItemKey);
                        return handlerInvoker(hc);
                    };

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ValueTask<TResponse> InvokeLight(IServiceProvider sp, ISpace space, TRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
#if DEBUG
            if (!HasLightInvoker)
            {
                var ctxDbg = HandlerContext<TRequest>.Create(sp, request, ct);

                return handlerInvoker(ctxDbg).AwaitAndReturnHandlerInvoke(ctxDbg);
            }
#endif
            var lctx = new LightHandlerContext<TRequest>(request, sp, space, ct);
            return lightInvoker(in lctx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<TResponse> Invoke(HandlerContext<TRequest> handlerContext)
        {
            handlerContext.CancellationToken.ThrowIfCancellationRequested();

            if (!hasPipelines)
            {
                return handlerInvoker(handlerContext);
            }

            EnsureComposed();

            var pipelineContext = handlerContext.ToPipelineContext();
            pipelineContext.SetItem(HandlerContextItemKey, handlerContext);

            var vt = cachedRootDelegate(pipelineContext);
            if (vt.IsCompletedSuccessfully)
            {
                PipelineContextPool<TRequest>.Return(pipelineContext);
                return vt;
            }
            return vt.AwaitAndReturnPipelineInvoke(pipelineContext);
        }

        public ValueTask<object> InvokeObject(HandlerContextStruct handlerContext)
        {
            if (HasLightInvoker)
            {
                var vtLight = InvokeLight(handlerContext.ServiceProvider, handlerContext.Space, (TRequest)handlerContext.Request, handlerContext.CancellationToken);
                if (vtLight.IsCompletedSuccessfully)
                {
                    return new ValueTask<object>(vtLight.Result!);
                }
                return AwaitLight(vtLight);
                static async ValueTask<object> AwaitLight(ValueTask<TResponse> t) { var r = await t; return (object)r!; }
            }

            if (!hasPipelines)
            {
                var ctxFast = HandlerContext<TRequest>.Create(handlerContext);
                var vtFast = handlerInvoker(ctxFast);
                if (vtFast.IsCompletedSuccessfully)
                {
                    HandlerContextPool<TRequest>.Return(ctxFast);
                    return new ValueTask<object>(vtFast.Result!);
                }
                return AwaitFast(vtFast, ctxFast);
                static async ValueTask<object> AwaitFast(ValueTask<TResponse> t, HandlerContext<TRequest> c) { try { return (object)await t; } finally { HandlerContextPool<TRequest>.Return(c); } }
            }

            var ctx = HandlerContext<TRequest>.Create(handlerContext);
            var vt = Invoke(ctx);
            if (vt.IsCompletedSuccessfully)
            {
                var result = (object)vt.Result!;
                HandlerContextPool<TRequest>.Return(ctx);
                return new ValueTask<object>(result);
            }
            return Await(vt, ctx);
            static async ValueTask<object> Await(ValueTask<TResponse> t, HandlerContext<TRequest> c) { try { return (object)await t; } finally { HandlerContextPool<TRequest>.Return(c); } }
        }
    }
}