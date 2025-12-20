using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction.Context;
using Space.Abstraction.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Space.Abstraction.Registry;

/// <summary>
/// Shared registration helper methods used by both root aggregator and assembly registrations.
/// Eliminates code duplication in generated templates.
/// </summary>
public static class SpaceRegistrationHelpers
{
    /// <summary>
    /// Register a pipeline-free handler with light invoker optimization.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterLightHandler<THandler, TRequest, TResponse>(
        SpaceRegistry registry,
        IServiceProvider sp,
        bool isSingleton,
        string name,
        Func<THandler, HandlerContext<TRequest>, ValueTask<TResponse>> body)
    {
        THandler singleton = isSingleton ? sp.GetRequiredService<THandler>() : default!;

        HandlerInvoker<TRequest, TResponse> normal = ctx =>
        {
            var inst = isSingleton ? singleton : ctx.ServiceProvider.GetRequiredService<THandler>();
            return body(inst, ctx);
        };

        LightHandlerInvoker<TRequest, TResponse> light = SpaceGeneratorRuntimeHelpers.CreateLightInvoker<TRequest, THandler, TResponse>(
            singleton, isSingleton, body);

        registry.RegisterLightHandler(normal, name, light);
    }

    /// <summary>
    /// Register a handler with pipelines using the generic entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterHandler<THandler, TRequest, TResponse>(
        SpaceRegistry registry,
        IServiceProvider sp,
        bool isSingleton,
        string name,
        Func<THandler, HandlerContext<TRequest>, ValueTask<TResponse>> body,
        IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelines = null)
    {
        THandler singleton = isSingleton ? sp.GetRequiredService<THandler>() : default!;

        HandlerInvoker<TRequest, TResponse> invoker = ctx =>
        {
            var inst = isSingleton ? singleton : ctx.ServiceProvider.GetRequiredService<THandler>();
            return body(inst, ctx);
        };

        registry.RegisterHandler(invoker, name, pipelines, null);
    }

    /// <summary>
    /// Register a handler with both handler pipelines and global pipelines.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterHandlerWithGlobalPipelines<THandler, TRequest, TResponse>(
        SpaceRegistry registry,
        IServiceProvider sp,
        bool isSingleton,
        string name,
        Func<THandler, HandlerContext<TRequest>, ValueTask<TResponse>> body,
        IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelines,
        IEnumerable<(GlobalPipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> globalPipelines)
    {
        THandler singleton = isSingleton ? sp.GetRequiredService<THandler>() : default!;

        HandlerInvoker<TRequest, TResponse> invoker = ctx =>
        {
            var inst = isSingleton ? singleton : ctx.ServiceProvider.GetRequiredService<THandler>();
            return body(inst, ctx);
        };

        registry.RegisterHandlerWithGlobalPipelines(invoker, name, pipelines, globalPipelines);
    }

    /// <summary>
    /// Register a handler with exactly one pipeline using specialized entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterSinglePipelineHandler<THandler, TRequest, TResponse>(
        SpaceRegistry registry,
        IServiceProvider sp,
        bool isSingleton,
        string name,
        Func<THandler, HandlerContext<TRequest>, ValueTask<TResponse>> body,
        PipelineInvoker<TRequest, TResponse> pipeline)
    {
        THandler singleton = isSingleton ? sp.GetRequiredService<THandler>() : default!;

        HandlerInvoker<TRequest, TResponse> invoker = ctx =>
        {
            var inst = isSingleton ? singleton : ctx.ServiceProvider.GetRequiredService<THandler>();
            return body(inst, ctx);
        };

        registry.RegisterSinglePipelineHandler(invoker, name, pipeline);
    }

    /// <summary>
    /// Register a handler with exactly two pipelines using specialized entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterTwoPipelinesHandler<THandler, TRequest, TResponse>(
        SpaceRegistry registry,
        IServiceProvider sp,
        bool isSingleton,
        string name,
        Func<THandler, HandlerContext<TRequest>, ValueTask<TResponse>> body,
        PipelineInvoker<TRequest, TResponse> pipeline1,
        PipelineInvoker<TRequest, TResponse> pipeline2)
    {
        THandler singleton = isSingleton ? sp.GetRequiredService<THandler>() : default!;

        HandlerInvoker<TRequest, TResponse> invoker = ctx =>
        {
            var inst = isSingleton ? singleton : ctx.ServiceProvider.GetRequiredService<THandler>();
            return body(inst, ctx);
        };

        registry.RegisterTwoPipelinesHandler(invoker, name, pipeline1, pipeline2);
    }

    /// <summary>
    /// Register a handler with exactly three pipelines using specialized entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterThreePipelinesHandler<THandler, TRequest, TResponse>(
        SpaceRegistry registry,
        IServiceProvider sp,
        bool isSingleton,
        string name,
        Func<THandler, HandlerContext<TRequest>, ValueTask<TResponse>> body,
        PipelineInvoker<TRequest, TResponse> pipeline1,
        PipelineInvoker<TRequest, TResponse> pipeline2,
        PipelineInvoker<TRequest, TResponse> pipeline3)
    {
        THandler singleton = isSingleton ? sp.GetRequiredService<THandler>() : default!;

        HandlerInvoker<TRequest, TResponse> invoker = ctx =>
        {
            var inst = isSingleton ? singleton : ctx.ServiceProvider.GetRequiredService<THandler>();
            return body(inst, ctx);
        };

        registry.RegisterThreePipelinesHandler(invoker, name, pipeline1, pipeline2, pipeline3);
    }

    /// <summary>
    /// Register a pipeline for an existing handler.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterPipeline<TPipeline, TRequest, TResponse>(
        SpaceRegistry registry,
        IServiceProvider sp,
        bool isSingleton,
        string handlerName,
        PipelineConfig config,
        Func<TPipeline, PipelineContext<TRequest>, PipelineDelegate<TRequest, TResponse>, ValueTask<TResponse>> invoker)
    {
        if (isSingleton)
        {
            var pipeInstance = sp.GetRequiredService<TPipeline>();
            registry.RegisterPipeline<TRequest, TResponse>(handlerName, config, (ctx, next) => invoker(pipeInstance, ctx, next));
        }
        else
        {
            registry.RegisterPipeline<TRequest, TResponse>(handlerName, config, (ctx, next) =>
            {
                var pipe = ctx.ServiceProvider.GetRequiredService<TPipeline>();
                return invoker(pipe, ctx, next);
            });
        }
    }

    /// <summary>
    /// Register a global pipeline.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterGlobalPipeline<TGlobalPipeline, TRequest, TResponse>(
        SpaceRegistry registry,
        IServiceProvider sp,
        bool isSingleton,
        GlobalPipelineConfig config,
        Func<TGlobalPipeline, PipelineContext<TRequest>, PipelineDelegate<TRequest, TResponse>, ValueTask<TResponse>> invoker)
    {
        if (isSingleton)
        {
            var gpInstance = sp.GetRequiredService<TGlobalPipeline>();
            registry.RegisterGlobalPipeline<TRequest, TResponse>(config, (ctx, next) => invoker(gpInstance, ctx, next));
        }
        else
        {
            registry.RegisterGlobalPipeline<TRequest, TResponse>(config, (ctx, next) =>
            {
                var gp = ctx.ServiceProvider.GetRequiredService<TGlobalPipeline>();
                return invoker(gp, ctx, next);
            });
        }
    }

    /// <summary>
    /// Register a notification handler.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterNotification<THandler, TRequest>(
        SpaceRegistry registry,
        IServiceProvider sp,
        bool isSingleton,
        Func<THandler, NotificationContext<TRequest>, ValueTask> call)
    {
        if (isSingleton)
        {
            var inst = sp.GetRequiredService<THandler>();
            registry.RegisterNotification<TRequest>(ctx => call(inst, ctx));
        }
        else
        {
            registry.RegisterNotification<TRequest>(ctx =>
            {
                var h = ctx.ServiceProvider.GetRequiredService<THandler>();
                return call(h, ctx);
            });
        }
    }

    /// <summary>
    /// Register a module for a handler.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterModule<TRequest, TResponse>(
        SpaceRegistry registry,
        string handlerName,
        string moduleName,
        string profileName)
    {
        registry.RegisterModule<TRequest, TResponse>(moduleName, handlerName, profileName);
    }

    /// <summary>
    /// Create a pipeline invoker from a pipeline class method.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PipelineInvoker<TRequest, TResponse> CreatePipelineInvoker<TPipeline, TRequest, TResponse>(
        IServiceProvider sp,
        bool isSingleton,
        Func<TPipeline, PipelineContext<TRequest>, PipelineDelegate<TRequest, TResponse>, ValueTask<TResponse>> invoker)
    {
        if (isSingleton)
        {
            var pipeInstance = sp.GetRequiredService<TPipeline>();
            return (ctx, next) => invoker(pipeInstance, ctx, next);
        }

        return (ctx, next) =>
        {
            var pipe = ctx.ServiceProvider.GetRequiredService<TPipeline>();
            return invoker(pipe, ctx, next);
        };
    }
}
