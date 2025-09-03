using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction.Registry.Dispatchers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction.Registry;

public partial class SpaceRegistry
{
    private readonly IServiceProvider serviceProvider;
    private readonly HandlerRegistry handlerRegistry;
    private readonly NotificationRegistry notificationRegistry;
    private readonly INotificationDispatcher notificationDispatcher;
    private readonly ModuleFactory moduleFactory;

    public SpaceRegistry(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;

        notificationDispatcher = serviceProvider.GetService<INotificationDispatcher>() ?? new SequentialNotificationDispatcher();
        notificationRegistry = new NotificationRegistry(notificationDispatcher);
        handlerRegistry = new HandlerRegistry(serviceProvider);
        moduleFactory = serviceProvider.GetRequiredService<ModuleFactory>();
    }

    public void RegisterPipeline<TRequest, TResponse>(string handlerName, PipelineConfig pipelineConfig,
        Func<PipelineContext<TRequest>, PipelineDelegate<TRequest, TResponse>, ValueTask<TResponse>> pipeline)
    {
        handlerRegistry.RegisterPipeline(handlerName, pipelineConfig, pipeline);
    }

    public void RegisterHandler<TRequest, TResponse>(
        Func<HandlerContext<TRequest>, ValueTask<TResponse>> handler,
        string name = "",
        IEnumerable<Func<PipelineContext<TRequest>, PipelineDelegate<TRequest, TResponse>, ValueTask<TResponse>>> pipelines = null)
    {
        handlerRegistry.RegisterHandler(handler, name, pipelines);
    }

    public void RegisterModule<TRequest, TResponse>(string moduleName, string handlerName = "")
    {
        var moduleType = serviceProvider.GetKeyedService<Type>(moduleName);

        var masterClass = serviceProvider.GetService(moduleType);

        int order = 0;
        if (masterClass is SpaceModule module)
        {
            order = module.PipelineOrder;
        }

        handlerRegistry.RegisterPipeline<TRequest, TResponse>(handlerName, new PipelineConfig(order),
            (ctx, next) =>
            {
                return moduleFactory.Invoke(moduleName, ctx, next);
            });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> DispatchHandler<TRequest, TResponse>(HandlerContext<TRequest> ctx, string name = "")
    {
        return handlerRegistry.DispatchHandler<TRequest, TResponse>(ctx, name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<object> DispatchHandler(object request, string name = "", CancellationToken ct = default)
    {
        var task = handlerRegistry.DispatchHandler(request, name, ct);

        return task;
    }

    public void RegisterNotification<TRequest>(Func<NotificationContext<TRequest>, ValueTask> handler, string name = "")
    {
        notificationRegistry.RegisterNotification(handler, name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask DispatchNotification<TRequest>(NotificationContext<TRequest> ctx, string name = "")
    {
        return notificationRegistry.DispatchNotification(ctx, name);
    }


    public void CompleteRegistration()
    {
        handlerRegistry.CompleteRegistration();
        notificationRegistry.CompleteRegistration();
    }

    internal interface IObjectHandlerEntry
    {
        ValueTask<object> InvokeObject(HandlerContextStruct handlerContext);
    }

    internal struct PipelineContainer<TRequest, TResponse>(PipelineConfig pipelineConfig, Func<PipelineContext<TRequest>, PipelineDelegate<TRequest, TResponse>, ValueTask<TResponse>> pipelineHandler)
    {
        internal PipelineConfig PipelineConfig { get; set; } = pipelineConfig;
        internal Func<PipelineContext<TRequest>, PipelineDelegate<TRequest, TResponse>, ValueTask<TResponse>> PipelineHandler { get; set; } = pipelineHandler;
    }


    internal static (Type, string) GenerateKey(Type requestType, string name = null)
    {
        return (requestType, name ?? string.Empty);
    }

    internal static (Type, string) GenerateKey<TRequest>(string name = null)
    {
        return GenerateKey(typeof(TRequest), name);
    }
}