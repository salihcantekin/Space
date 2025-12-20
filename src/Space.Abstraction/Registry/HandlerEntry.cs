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
    /// <summary>
    /// Base handler entry class for handlers with dynamic pipeline composition.
    /// For better performance, use specialized entries: LightHandlerEntry, SinglePipelineEntry, etc.
    /// </summary>
    internal abstract class HandlerEntry<TRequest, TResponse> : IObjectHandlerEntry
    {
        protected readonly HandlerInvoker<TRequest, TResponse> handlerInvoker;
        protected readonly LightHandlerInvoker<TRequest, TResponse> lightInvoker;
        
        // Pipeline storage - separating handler pipelines from global pipelines
        private readonly List<(int Order, PipelineInvoker<TRequest, TResponse> Invoker)> handlerPipelines;
        private readonly List<(int Order, int ExecutionStage, PipelineInvoker<TRequest, TResponse> Invoker)> globalPipelines;
        private bool hasPipelines;

        private readonly object composeLock = new();
        private PipelineInvoker<TRequest, TResponse>[] orderedPipelines;
        private bool compositionDirty = true;

        // Composed pipeline chain
        private PipelineDelegate<TRequest, TResponse> cachedRootDelegate;
        private HandlerInvoker<TRequest, TResponse> composedInvoke;

        // Single-pipeline optimization fields
        private PipelineInvoker<TRequest, TResponse> singlePipelineInvoker;
        private PipelineDelegate<TRequest, TResponse> cachedFinalDelegate;

        // Virtual properties so specialized entries can override
        internal virtual bool IsPipelineFree => !hasPipelines;
        internal virtual bool HasLightInvoker => lightInvoker != null && !hasPipelines;

        protected HandlerEntry(
            HandlerInvoker<TRequest, TResponse> handlerInvoker,
            LightHandlerInvoker<TRequest, TResponse> lightInvoker,
            IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelineInvokers,
            IEnumerable<(GlobalPipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> globalPipelineInvokers = null)
        {
            this.handlerInvoker = handlerInvoker;
            this.lightInvoker = lightInvoker;

            handlerPipelines = [];
            globalPipelines = [];

            // Add handler-specific pipelines
            if (pipelineInvokers != null)
            {
                foreach (var p in pipelineInvokers)
                {
                    handlerPipelines.Add((p.config.Order, p.invoker));
                }
            }

            // Add global pipelines
            if (globalPipelineInvokers != null)
            {
                foreach (var gp in globalPipelineInvokers)
                {
                    globalPipelines.Add((gp.config.Order, gp.config.ExecutionStage, gp.invoker));
                }
            }

            hasPipelines = handlerPipelines.Count > 0 || globalPipelines.Count > 0;
            
            // Pre-compose at construction time for performance
            if (hasPipelines)
            {
                EnsureComposed();
            }
            else
            {
                composedInvoke = handlerInvoker;
                compositionDirty = false;
            }
        }

        internal void AddPipeline(PipelineInvoker<TRequest, TResponse> invoker, PipelineConfig pipelineConfig)
        {
            handlerPipelines.Add((pipelineConfig.Order, invoker));
            hasPipelines = true;
            orderedPipelines = null;
            compositionDirty = true;
        }

        private PipelineInvoker<TRequest, TResponse>[] GetOrdered()
        {
            if (orderedPipelines != null)
                return orderedPipelines;

            lock (composeLock)
            {
                if (orderedPipelines != null)
                    return orderedPipelines;

                // Build properly ordered pipeline list
                var result = new List<PipelineInvoker<TRequest, TResponse>>();

                // Stage 0: BeforeHandler global pipelines (execute first, outer)
                var stage0 = globalPipelines
                    .Where(gp => gp.ExecutionStage == 0)
                    .OrderBy(gp => gp.Order)
                    .Select(gp => gp.Invoker);
                result.AddRange(stage0);
                
                // Handler-specific pipelines
                var handlerPipes = handlerPipelines
                    .OrderBy(hp => hp.Order)
                    .Select(hp => hp.Invoker);
                result.AddRange(handlerPipes);
                
                // Stage 1: BeforeHandlerInner global pipelines
                var stage1 = globalPipelines
                    .Where(gp => gp.ExecutionStage == 1)
                    .OrderBy(gp => gp.Order)
                    .Select(gp => gp.Invoker);
                result.AddRange(stage1);
                
                // Stage 2 & 3: AfterHandlerInner and AfterHandler (reverse order)
                var stage2 = globalPipelines
                    .Where(gp => gp.ExecutionStage == 2)
                    .OrderByDescending(gp => gp.Order)
                    .Select(gp => gp.Invoker);
                result.AddRange(stage2);

                var stage3 = globalPipelines
                    .Where(gp => gp.ExecutionStage == 3)
                    .OrderByDescending(gp => gp.Order)
                    .Select(gp => gp.Invoker);
                result.AddRange(stage3);

                orderedPipelines = [.. result];
            }

            return orderedPipelines;
        }

        protected void EnsureComposed()
        {
            if (!compositionDirty)
                return;

            lock (composeLock)
            {
                if (!compositionDirty)
                    return;

                cachedRootDelegate = null;
                composedInvoke = null;
                singlePipelineInvoker = null;
                cachedFinalDelegate = null;

                var totalPipelines = handlerPipelines.Count + globalPipelines.Count;

                if (totalPipelines == 0)
                {
                    composedInvoke = handlerInvoker;
                }
                else
                {
                    var ordered = GetOrdered();

                    // Final delegate: invoke the handler with HandlerContextRef
                    // Check cancellation before invoking handler (after all pipelines)
                    cachedFinalDelegate = pc =>
                    {
                        pc.CancellationToken.ThrowIfCancellationRequested();
                        return handlerInvoker(pc.HandlerContextRef);
                    };

                    if (ordered.Length == 1)
                    {
                        // Single pipeline fast path
                        singlePipelineInvoker = ordered[0];

                        composedInvoke = (ctx) =>
                        {
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
                        // Compose full chain
                        PipelineDelegate<TRequest, TResponse> root = cachedFinalDelegate;

                        for (int i = ordered.Length - 1; i >= 0; i--)
                        {
                            var current = ordered[i];
                            var next = root;
                            root = (pctx) => current(pctx, next);
                        }

                        cachedRootDelegate = root;

                        composedInvoke = (ctx) =>
                        {
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
        internal virtual ValueTask<TResponse> InvokeLight(IServiceProvider sp, ISpace space, TRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            
            if (lightInvoker == null)
            {
                var ctxDbg = HandlerContext<TRequest>.Create(sp, request, ct);
                return handlerInvoker(ctxDbg).AwaitAndReturnHandlerInvoke(ctxDbg);
            }
            
            var lctx = new LightHandlerContext<TRequest>(request, sp, space, ct);
            return lightInvoker(in lctx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual ValueTask<TResponse> Invoke(HandlerContext<TRequest> handlerContext)
        {
            handlerContext.CancellationToken.ThrowIfCancellationRequested();
            
            if (compositionDirty)
                EnsureComposed();
            
            return composedInvoke(handlerContext);
        }

        public ValueTask<object> InvokeObject(HandlerContextStruct handlerContext)
        {
            if (HasLightInvoker)
            {
                var vtLight = InvokeLight(handlerContext.ServiceProvider, handlerContext.Space, (TRequest)handlerContext.Request, handlerContext.CancellationToken);

                if (vtLight.IsCompletedSuccessfully)
                    return new ValueTask<object>(vtLight.Result!);

                return AwaitLight(vtLight);

                static async ValueTask<object> AwaitLight(ValueTask<TResponse> t)
                {
                    var r = await t;
                    return (object)r!;
                }
            }

            if (IsPipelineFree)
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

    /// <summary>
    /// Handler entry for Singleton lifetime. Currently same as base but kept for future optimizations.
    /// </summary>
    internal sealed class SingletonHandlerEntry<TRequest, TResponse> : HandlerEntry<TRequest, TResponse>
    {
        public SingletonHandlerEntry(HandlerInvoker<TRequest, TResponse> handlerInvoker,
                                     LightHandlerInvoker<TRequest, TResponse> lightInvoker,
                                     IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelineInvokers,
                                     IEnumerable<(GlobalPipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> globalPipelineInvokers = null)
            : base(handlerInvoker, lightInvoker, pipelineInvokers, globalPipelineInvokers) { }
    }

    /// <summary>
    /// Handler entry for Scoped/Transient lifetime.
    /// </summary>
    internal sealed class ScopedHandlerEntry<TRequest, TResponse> : HandlerEntry<TRequest, TResponse>
    {
        public ScopedHandlerEntry(HandlerInvoker<TRequest, TResponse> handlerInvoker,
                                  LightHandlerInvoker<TRequest, TResponse> lightInvoker,
                                  IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelineInvokers,
                                  IEnumerable<(GlobalPipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> globalPipelineInvokers = null)
            : base(handlerInvoker, lightInvoker, pipelineInvokers, globalPipelineInvokers) { }
    }
}
