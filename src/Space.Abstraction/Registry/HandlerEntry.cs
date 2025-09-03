using Space.Abstraction.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Space.Abstraction.Registry;

public partial class SpaceRegistry
{
    internal sealed class HandlerEntry<TRequest, TResponse>(
        Func<HandlerContext<TRequest>, ValueTask<TResponse>> handler,
        IEnumerable<Func<PipelineContext<TRequest>, PipelineDelegate<TRequest, TResponse>, ValueTask<TResponse>>> pipelines) : IObjectHandlerEntry
    {
        private readonly List<PipelineContainer<TRequest, TResponse>> pipelines = pipelines != null
            ? [.. pipelines.Select((p, i) => new PipelineContainer<TRequest, TResponse>(new PipelineConfig(i), p))]
            : [];

        private PipelineDelegate<TRequest, TResponse> cachedPipelineDelegate;
        private readonly object pipelineLock = new();

        internal void AddPipeline(Func<PipelineContext<TRequest>, PipelineDelegate<TRequest, TResponse>, ValueTask<TResponse>> pipeline, PipelineConfig pipelineConfig)
        {
            pipelines.Add(new PipelineContainer<TRequest, TResponse>(pipelineConfig, pipeline));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<TResponse> Invoke(HandlerContext<TRequest> handlerContext)
        {
            handlerContext.CancellationToken.ThrowIfCancellationRequested();

            ValueTask<TResponse> task;
            if (pipelines.Count == 0)
            {
                task = handler(handlerContext);
            }
            else
            {
                var pipelineContext = handlerContext.ToPipelineContext();
                if (cachedPipelineDelegate != null)
                {
                    task = cachedPipelineDelegate(pipelineContext);
                }
                else
                {
                    lock (pipelineLock)
                    {
                        if (cachedPipelineDelegate == null)
                        {
                            PipelineDelegate<TRequest, TResponse> next = (ctx) => handler(handlerContext);

                            foreach (var current in pipelines.OrderByDescending(i => i.PipelineConfig.Order))
                            {
                                var currentNext = next;
                                next = (ctx) => current.PipelineHandler(ctx, currentNext);
                            }

                            cachedPipelineDelegate = next;
                        }
                    }
                    task = cachedPipelineDelegate(pipelineContext);
                }
            }

            return task;
        }


        public ValueTask<object> InvokeObject(HandlerContextStruct handlerContext)
        {
            var ctx = HandlerContext<TRequest>.Create(handlerContext);
            var task = Invoke(ctx).BoxValueTask();

            if (task.IsCompletedSuccessfully)
            {
                HandlerContextPool<TRequest>.Return(ctx);
                return task;
            }

            return task.AwaitAndReturnHandlerObject(ctx);
        }
    }
}