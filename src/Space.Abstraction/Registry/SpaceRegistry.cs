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

    // Records the lifetime used when registering handlers (set by DI generator)
    public ServiceLifetime HandlerLifetime { get; internal set; } = ServiceLifetime.Scoped;

    public SpaceRegistry(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;

        notificationDispatcher = serviceProvider.GetService<INotificationDispatcher>() ?? new SequentialNotificationDispatcher();
        notificationRegistry = new NotificationRegistry(notificationDispatcher);
        handlerRegistry = new HandlerRegistry(serviceProvider);
        moduleFactory = serviceProvider.GetRequiredService<ModuleFactory>();

        CurrentRegistry = this; // make available for fast object dispatch
    }

    public void RegisterPipeline<TRequest, TResponse>(string handlerName, PipelineConfig pipelineConfig,
        PipelineInvoker<TRequest, TResponse> pipeline)
    {
        handlerRegistry.RegisterPipeline(handlerName, pipelineConfig, pipeline);
    }

    public void RegisterHandler<TRequest, TResponse>(
        HandlerInvoker<TRequest, TResponse> handler,
        string name = "",
        IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelines = null)
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
            (ctx, next) => moduleFactory.Invoke(moduleName, ctx, next));
    }

    // Lightweight handler entry access for fast path
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetHandlerEntry<TRequest, TResponse>(string name, out HandlerEntry<TRequest, TResponse> entry)
        => handlerRegistry.TryGetHandlerEntry<TRequest, TResponse>(name, out entry);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> DispatchHandler<TRequest, TResponse>(IServiceProvider execProvider, HandlerContext<TRequest> ctx, string name = "")
        => handlerRegistry.DispatchHandler<TRequest, TResponse>(execProvider, ctx, name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> DispatchHandler<TRequest, TResponse>(HandlerContext<TRequest> ctx, string name = "")
        => handlerRegistry.DispatchHandler<TRequest, TResponse>(ctx.ServiceProvider, ctx, name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<object> DispatchHandler(object request, string name = "", CancellationToken ct = default)
        => handlerRegistry.DispatchHandler(request, name, ct);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<object> DispatchHandler(object request, string name, IServiceProvider execProvider, CancellationToken ct = default)
        => handlerRegistry.DispatchHandler(request, name, execProvider, ct);

    public void RegisterNotification<TRequest>(Func<NotificationContext<TRequest>, ValueTask> handler, string name = "")
        => notificationRegistry.RegisterNotification(handler, name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask DispatchNotification<TRequest>(NotificationContext<TRequest> ctx, string name = "")
        => notificationRegistry.DispatchNotification(ctx, name);

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
        => (requestType, name ?? string.Empty);

    internal static (Type, string) GenerateKey<TRequest>(string name = null)
        => GenerateKey(typeof(TRequest), name);

    internal static SpaceRegistry CurrentRegistry; // for fast object dispatch
}