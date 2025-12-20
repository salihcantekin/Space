using System.Runtime.CompilerServices;

namespace Space.Abstraction.Registry;

/// <summary>
/// Lightweight struct container for pipeline configuration and invoker.
/// Eliminates heap allocation compared to class-based container.
/// </summary>
public readonly struct PipelineContainer<TRequest, TResponse>
{
    public readonly PipelineConfig Config;
    public readonly PipelineInvoker<TRequest, TResponse> Invoker;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PipelineContainer(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)
    {
        Config = config;
        Invoker = invoker;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PipelineContainer(int order, PipelineInvoker<TRequest, TResponse> invoker)
    {
        Config = new PipelineConfig { Order = order };
        Invoker = invoker;
    }
}

/// <summary>
/// Lightweight struct container for global pipeline configuration and invoker.
/// Eliminates heap allocation compared to class-based container.
/// </summary>
public readonly struct GlobalPipelineContainer<TRequest, TResponse>
{
    public readonly GlobalPipelineConfig Config;
    public readonly PipelineInvoker<TRequest, TResponse> Invoker;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GlobalPipelineContainer(GlobalPipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)
    {
        Config = config;
        Invoker = invoker;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GlobalPipelineContainer(int order, int executionStage, PipelineInvoker<TRequest, TResponse> invoker)
    {
        Config = new GlobalPipelineConfig { Order = order, ExecutionStage = executionStage };
        Invoker = invoker;
    }
}

/// <summary>
/// Combined pipeline container that can hold either handler-specific or global pipeline.
/// Used in the ordered pipeline array to avoid separate collections.
/// </summary>
public readonly struct OrderedPipelineContainer<TRequest, TResponse>
{
    public readonly int Order;
    public readonly int ExecutionStage; // 0 for handler-specific pipelines
    public readonly PipelineInvoker<TRequest, TResponse> Invoker;
    public readonly bool IsGlobal;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedPipelineContainer(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)
    {
        Order = config.Order;
        ExecutionStage = 0;
        Invoker = invoker;
        IsGlobal = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedPipelineContainer(GlobalPipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)
    {
        Order = config.Order;
        ExecutionStage = config.ExecutionStage;
        Invoker = invoker;
        IsGlobal = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedPipelineContainer(int order, int executionStage, PipelineInvoker<TRequest, TResponse> invoker, bool isGlobal)
    {
        Order = order;
        ExecutionStage = executionStage;
        Invoker = invoker;
        IsGlobal = isGlobal;
    }
}
