using System;

namespace Space.Abstraction.Attributes;

/// <summary>
/// Marks a method as a global pipeline that executes for all handlers with matching TRequest and TResponse types.
/// Global pipelines can be ordered relative to handler-specific pipelines using ExecutionStage and Order.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class GlobalPipelineAttribute : Attribute, IPipelineConfig
{
    /// <summary>
    /// Order within the execution stage. Lower values execute first (outer pipelines).
    /// Default is 100.
    /// </summary>
    public int Order { get; set; } = 100;

    /// <summary>
    /// Determines when this global pipeline executes relative to handler-specific pipelines.
    /// Default is BeforeHandler.
    /// </summary>
    public GlobalPipelineExecutionStage ExecutionStage { get; set; } = GlobalPipelineExecutionStage.BeforeHandler;

    public GlobalPipelineAttribute() { }
}

/// <summary>
/// Defines the execution stage of a global pipeline relative to handler-specific pipelines.
/// </summary>
public enum GlobalPipelineExecutionStage
{
    /// <summary>
    /// Execute before any handler-specific pipelines (outermost).
    /// Order: Global(BeforeHandler) -> Handler Pipelines -> Handler -> Global(AfterHandler)
    /// Use case: Cross-cutting concerns like logging, authentication, validation
    /// </summary>
    BeforeHandler = 0,

    /// <summary>
    /// Execute after handler-specific pipelines but before the handler (innermost pre-handler).
    /// Order: Handler Pipelines -> Global(BeforeHandlerInner) -> Handler -> Global(AfterHandlerInner)
    /// Use case: Final pre-processing before handler execution
    /// </summary>
    BeforeHandlerInner = 1,

    /// <summary>
    /// Execute after the handler but before unwinding handler-specific pipelines (innermost post-handler).
    /// Order: Handler Pipelines -> Handler -> Global(AfterHandlerInner) -> unwinding handler pipelines
    /// Use case: Immediate post-processing of handler result
    /// </summary>
    AfterHandlerInner = 2,

    /// <summary>
    /// Execute after all handler-specific pipelines have unwound (outermost post-handler).
    /// Order: Handler -> Handler Pipelines (unwinding) -> Global(AfterHandler)
    /// Use case: Final response transformation, caching, metrics
    /// </summary>
    AfterHandler = 3
}
