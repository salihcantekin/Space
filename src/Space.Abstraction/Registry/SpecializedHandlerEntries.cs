using Space.Abstraction.Extensions;
using Space.Abstraction.Context;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction.Registry;

public partial class SpaceRegistry
{
    /// <summary>
    /// Optimized handler entry for handlers with NO pipelines.
    /// Avoids all pipeline composition overhead and context pooling for maximum performance.
    /// </summary>
    internal sealed class LightHandlerEntry<TRequest, TResponse> : HandlerEntry<TRequest, TResponse>
    {
        private readonly LightHandlerInvoker<TRequest, TResponse> _lightInvoker;

        public LightHandlerEntry(
            HandlerInvoker<TRequest, TResponse> handlerInvoker,
            LightHandlerInvoker<TRequest, TResponse> lightInvoker)
            : base(handlerInvoker, lightInvoker, null, null)
        {
            _lightInvoker = lightInvoker ?? throw new ArgumentNullException(nameof(lightInvoker));
        }

        /// <summary>
        /// Returns the underlying light invoker delegate for direct caching in Space.cs
        /// This enables bypassing virtual dispatch in the ultra-fast path.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal LightHandlerInvoker<TRequest, TResponse> GetLightInvoker() => _lightInvoker;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ValueTask<TResponse> Invoke(HandlerContext<TRequest> handlerContext)
        {
            handlerContext.CancellationToken.ThrowIfCancellationRequested();
            return handlerInvoker(handlerContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ValueTask<TResponse> InvokeLight(IServiceProvider sp, ISpace space, TRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var lctx = new LightHandlerContext<TRequest>(request, sp, space, ct);
            return _lightInvoker(in lctx);
        }

        internal override bool IsPipelineFree => true;
        internal override bool HasLightInvoker => true;
    }

    /// <summary>
    /// Optimized handler entry for handlers with exactly ONE pipeline.
    /// Inline invocation without delegate chain composition.
    /// </summary>
    internal sealed class SinglePipelineEntry<TRequest, TResponse> : HandlerEntry<TRequest, TResponse>
    {
        private readonly PipelineInvoker<TRequest, TResponse> _pipelineInvoker;
        private readonly PipelineDelegate<TRequest, TResponse> _finalDelegate;

        public SinglePipelineEntry(
            HandlerInvoker<TRequest, TResponse> handlerInvoker,
            PipelineInvoker<TRequest, TResponse> pipelineInvoker)
            : base(handlerInvoker, null, null, null)
        {
            _pipelineInvoker = pipelineInvoker ?? throw new ArgumentNullException(nameof(pipelineInvoker));
            _finalDelegate = pc => handlerInvoker(pc.HandlerContextRef);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ValueTask<TResponse> Invoke(HandlerContext<TRequest> handlerContext)
        {
            handlerContext.CancellationToken.ThrowIfCancellationRequested();

            var pc = PipelineContextPool<TRequest>.Get(
                handlerContext.Request,
                handlerContext.ServiceProvider,
                handlerContext.Space,
                handlerContext.CancellationToken);
            pc.HandlerContextRef = handlerContext;

            var vt = _pipelineInvoker(pc, _finalDelegate);
            
            if (vt.IsCompletedSuccessfully)
            {
                PipelineContextPool<TRequest>.Return(pc);
                return vt;
            }

            return vt.AwaitAndReturnPipelineInvoke(pc);
        }

        internal override bool IsPipelineFree => false;
        internal override bool HasLightInvoker => false;
    }

    /// <summary>
    /// Optimized handler entry for handlers with exactly TWO pipelines.
    /// Unrolled invocation chain without delegate array iteration.
    /// </summary>
    internal sealed class TwoPipelinesEntry<TRequest, TResponse> : HandlerEntry<TRequest, TResponse>
    {
        private readonly PipelineInvoker<TRequest, TResponse> _pipeline1;
        private readonly PipelineInvoker<TRequest, TResponse> _pipeline2;
        private readonly PipelineDelegate<TRequest, TResponse> _finalDelegate;
        private readonly PipelineDelegate<TRequest, TResponse> _chain2;

        public TwoPipelinesEntry(
            HandlerInvoker<TRequest, TResponse> handlerInvoker,
            PipelineInvoker<TRequest, TResponse> pipeline1,
            PipelineInvoker<TRequest, TResponse> pipeline2)
            : base(handlerInvoker, null, null, null)
        {
            _pipeline1 = pipeline1 ?? throw new ArgumentNullException(nameof(pipeline1));
            _pipeline2 = pipeline2 ?? throw new ArgumentNullException(nameof(pipeline2));
            _finalDelegate = pc => handlerInvoker(pc.HandlerContextRef);
            _chain2 = pc => _pipeline2(pc, _finalDelegate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ValueTask<TResponse> Invoke(HandlerContext<TRequest> handlerContext)
        {
            handlerContext.CancellationToken.ThrowIfCancellationRequested();

            var pc = PipelineContextPool<TRequest>.Get(
                handlerContext.Request,
                handlerContext.ServiceProvider,
                handlerContext.Space,
                handlerContext.CancellationToken);
            pc.HandlerContextRef = handlerContext;

            var vt = _pipeline1(pc, _chain2);
            
            if (vt.IsCompletedSuccessfully)
            {
                PipelineContextPool<TRequest>.Return(pc);
                return vt;
            }

            return vt.AwaitAndReturnPipelineInvoke(pc);
        }

        internal override bool IsPipelineFree => false;
        internal override bool HasLightInvoker => false;
    }

    /// <summary>
    /// Optimized handler entry for handlers with exactly THREE pipelines.
    /// Unrolled invocation chain without delegate array iteration.
    /// </summary>
    internal sealed class ThreePipelinesEntry<TRequest, TResponse> : HandlerEntry<TRequest, TResponse>
    {
        private readonly PipelineInvoker<TRequest, TResponse> _pipeline1;
        private readonly PipelineInvoker<TRequest, TResponse> _pipeline2;
        private readonly PipelineInvoker<TRequest, TResponse> _pipeline3;
        private readonly PipelineDelegate<TRequest, TResponse> _finalDelegate;
        private readonly PipelineDelegate<TRequest, TResponse> _chain3;
        private readonly PipelineDelegate<TRequest, TResponse> _chain2;

        public ThreePipelinesEntry(
            HandlerInvoker<TRequest, TResponse> handlerInvoker,
            PipelineInvoker<TRequest, TResponse> pipeline1,
            PipelineInvoker<TRequest, TResponse> pipeline2,
            PipelineInvoker<TRequest, TResponse> pipeline3)
            : base(handlerInvoker, null, null, null)
        {
            _pipeline1 = pipeline1 ?? throw new ArgumentNullException(nameof(pipeline1));
            _pipeline2 = pipeline2 ?? throw new ArgumentNullException(nameof(pipeline2));
            _pipeline3 = pipeline3 ?? throw new ArgumentNullException(nameof(pipeline3));
            _finalDelegate = pc => handlerInvoker(pc.HandlerContextRef);
            _chain3 = pc => _pipeline3(pc, _finalDelegate);
            _chain2 = pc => _pipeline2(pc, _chain3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ValueTask<TResponse> Invoke(HandlerContext<TRequest> handlerContext)
        {
            handlerContext.CancellationToken.ThrowIfCancellationRequested();

            var pc = PipelineContextPool<TRequest>.Get(
                handlerContext.Request,
                handlerContext.ServiceProvider,
                handlerContext.Space,
                handlerContext.CancellationToken);
            pc.HandlerContextRef = handlerContext;

            var vt = _pipeline1(pc, _chain2);
            
            if (vt.IsCompletedSuccessfully)
            {
                PipelineContextPool<TRequest>.Return(pc);
                return vt;
            }

            return vt.AwaitAndReturnPipelineInvoke(pc);
        }

        internal override bool IsPipelineFree => false;
        internal override bool HasLightInvoker => false;
    }
}
