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
    internal abstract class HandlerEntry<TRequest, TResponse> : IObjectHandlerEntry
    {
        protected readonly HandlerInvoker<TRequest, TResponse> handlerInvoker;
        protected readonly LightHandlerInvoker<TRequest, TResponse> lightInvoker;
        protected readonly List<PipelineContainer> pipelines;
        protected bool hasPipelines; // must be mutable to reflect late pipeline registration

        private readonly object composeLock = new();
        private PipelineContainer[] orderedPipelines; // cached ordered list
        private bool compositionDirty = true;

        // Composed root pipeline delegate (for 1+ pipelines)
        private PipelineDelegate<TRequest, TResponse> cachedRootDelegate;

        // Single composed invoker over HandlerContext to minimize branching in Invoke
        private HandlerInvoker<TRequest, TResponse> composedInvoke;

        // Single-pipeline fast path fields
        private bool singlePipelineFast;
        private PipelineInvoker<TRequest, TResponse> singlePipelineInvoker;
        private PipelineDelegate<TRequest, TResponse> cachedFinalDelegate; // calls handlerInvoker with HandlerContextRef

        protected sealed class PipelineContainer
        {
            internal PipelineConfig Config { get; }
            internal PipelineInvoker<TRequest, TResponse> Invoker { get; }
            internal PipelineContainer(PipelineConfig pipelineConfig, PipelineInvoker<TRequest, TResponse> invoker)
            {
                Config = pipelineConfig;
                Invoker = invoker;
            }
        }

        internal bool IsPipelineFree => !hasPipelines;
        internal bool HasLightInvoker => lightInvoker != null && !hasPipelines;

        protected HandlerEntry(
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
            {
                return orderedPipelines;
            }

            lock (composeLock)
            {
                orderedPipelines ??= pipelines.OrderBy(p => p.Config.Order).ToArray();
            }

            return orderedPipelines;
        }

        protected void EnsureComposed()
        {
            if (!compositionDirty)
            {
                return;
            }

            lock (composeLock)
            {
                if (!compositionDirty)
                {
                    return;
                }

                cachedRootDelegate = null;
                composedInvoke = null;
                singlePipelineFast = false;
                singlePipelineInvoker = null;
                cachedFinalDelegate = null;

                if (!hasPipelines || pipelines.Count == 0)
                {
                    // No pipeline chain: composed is just the handler invoker
                    composedInvoke = handlerInvoker;
                }
                else
                {
                    var ordered = GetOrdered();

                    // final delegate: invoke the handler with the pre-set HandlerContextRef
                    cachedFinalDelegate = pc => handlerInvoker(pc.HandlerContextRef);

                    if (ordered.Length == 1)
                    {
                        singlePipelineFast = true;
                        singlePipelineInvoker = ordered[0].Invoker;

                        composedInvoke = (ctx) =>
                        {
                            // Inline ToPipelineContext to avoid extra call
                            var pc = PipelineContextPool<TRequest>.Get(ctx.Request, ctx.ServiceProvider, ctx.Space, ctx.CancellationToken);
                            pc.HandlerContextRef = ctx;

                            var vt = singlePipelineInvoker(pc, cachedFinalDelegate);
                            if (vt.IsCompletedSuccessfully)
                            {
                                PipelineContextPool<TRequest>.Return(pc);
                                return vt;
                            }
                            return vt.AwaitAndReturnPipelineInvoke(pc);
                        };
                    }
                    else
                    {
                        // Compose full chain including final handler (via HandlerContextRef)
                        PipelineDelegate<TRequest, TResponse> root = cachedFinalDelegate;

                        for (int i = ordered.Length - 1; i >= 0; i--)
                        {
                            var current = ordered[i];
                            var next = root;
                            root = (pctx) => current.Invoker(pctx, next);
                        }

                        cachedRootDelegate = root;

                        // Compose a single invoker over HandlerContext that handles PipelineContext pooling
                        composedInvoke = (ctx) =>
                        {
                            // Inline ToPipelineContext to avoid extra call
                            var pc = PipelineContextPool<TRequest>.Get(ctx.Request, ctx.ServiceProvider, ctx.Space, ctx.CancellationToken);
                            pc.HandlerContextRef = ctx;

                            var vt = cachedRootDelegate(pc);
                            if (vt.IsCompletedSuccessfully)
                            {
                                PipelineContextPool<TRequest>.Return(pc);
                                return vt;
                            }
                            return vt.AwaitAndReturnPipelineInvoke(pc);
                        };
                    }
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
        public virtual ValueTask<TResponse> Invoke(HandlerContext<TRequest> handlerContext)
        {
            handlerContext.CancellationToken.ThrowIfCancellationRequested();
            EnsureComposed();
            return composedInvoke(handlerContext);
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

                static async ValueTask<object> AwaitLight(ValueTask<TResponse> t)
                {
                    var r = await t;
                    return (object)r!;
                }
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

                static async ValueTask<object> AwaitFast(ValueTask<TResponse> t, HandlerContext<TRequest> c)
                {
                    try { return (object)await t; }
                    finally { HandlerContextPool<TRequest>.Return(c); }
                }
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

            static async ValueTask<object> Await(ValueTask<TResponse> t, HandlerContext<TRequest> c)
            {
                try { return (object)await t; }
                finally { HandlerContextPool<TRequest>.Return(c); }
            }
        }
    }

    // Specialized entries (currently behavior is the same because DI differences are handled by generator; kept for future specialization)
    internal sealed class SingletonHandlerEntry<TRequest, TResponse> : HandlerEntry<TRequest, TResponse>
    {
        public SingletonHandlerEntry(HandlerInvoker<TRequest, TResponse> handlerInvoker,
                                     LightHandlerInvoker<TRequest, TResponse> lightInvoker,
                                     IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelineInvokers)
            : base(handlerInvoker, lightInvoker, pipelineInvokers) { }

        // In future, override Invoke to use thread-static contexts or other singleton-specific tricks
    }

    internal sealed class ScopedHandlerEntry<TRequest, TResponse> : HandlerEntry<TRequest, TResponse>
    {
        public ScopedHandlerEntry(HandlerInvoker<TRequest, TResponse> handlerInvoker,
                                  LightHandlerInvoker<TRequest, TResponse> lightInvoker,
                                  IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelineInvokers)
            : base(handlerInvoker, lightInvoker, pipelineInvokers) { }
    }
}