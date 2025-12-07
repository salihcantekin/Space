namespace Space.Abstraction.Registry;

/// <summary>
/// Configuration for global pipelines including execution order and stage.
/// </summary>
public sealed class GlobalPipelineConfig
{
    /// <summary>
    /// Order within the execution stage (lower executes first)
    /// </summary>
    public int Order { get; set; } = 100;

    /// <summary>
    /// Execution stage relative to handler-specific pipelines (0-3)
    /// 0 = BeforeHandler, 1 = BeforeHandlerInner, 2 = AfterHandlerInner, 3 = AfterHandler
    /// </summary>
    public int ExecutionStage { get; set; } = 0;
}
