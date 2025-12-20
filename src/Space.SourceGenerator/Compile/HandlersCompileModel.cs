using Space.SourceGenerator.Extensions;

namespace Space.SourceGenerator.Compile;

/// <summary>
/// Execution mode determined at compile-time for optimal runtime dispatch.
/// </summary>
public enum HandlerExecutionMode
{
    /// <summary>No pipelines, no global pipelines, no modules - direct handler invocation.</summary>
    Pure = 0,
    
    /// <summary>Exactly one handler-specific pipeline, no global pipelines or modules.</summary>
    SinglePipeline = 1,
    
    /// <summary>Exactly two handler-specific pipelines, no global pipelines or modules.</summary>
    TwoPipelines = 2,
    
    /// <summary>Exactly three handler-specific pipelines, no global pipelines or modules.</summary>
    ThreePipelines = 3,
    
    /// <summary>Has global pipelines attached (with or without handler pipelines).</summary>
    WithGlobalPipelines = 4,
    
    /// <summary>4+ pipelines or has modules - uses generic composed pipeline chain.</summary>
    Generic = 5
}

public record HandlersCompileModel(string HandlerName) : BaseCompileModel
{
    public PipelineCompileModel[] PipelineCompileModels { get; set; } = [];

    public GlobalPipelineCompileModel[] GlobalPipelineCompileModels { get; set; } = [];

    public ModuleCompileModel[] ModuleCompileModels { get; set; } = [];

    // New: mark handler as default for its Req/Res pair
    public bool IsDefault { get; set; }

    /// <summary>
    /// Execution mode determined at compile-time. Set by HandlersCompileWrapperModel.Build().
    /// </summary>
    public HandlerExecutionMode ExecutionMode { get; set; } = HandlerExecutionMode.Pure;

    /// <summary>
    /// Integer representation of ExecutionMode for Scriban template comparison.
    /// </summary>
    public int ExecutionModeInt => (int)ExecutionMode;

    public bool IsReturnTypeEmpty => string.IsNullOrEmpty(ReturnTypeName);
    public bool IsTask => ReturnTaskTypeName.IsTask();
    public bool IsValueTask => ReturnTaskTypeName.IsValueTask();

    /// <summary>
    /// Total count of pipelines (handler-specific + global + modules)
    /// Used by Source Generator to select specialized handler entry types.
    /// </summary>
    public int TotalPipelineCount => PipelineCompileModels.Length + GlobalPipelineCompileModels.Length + ModuleCompileModels.Length;

    /// <summary>
    /// Only handler-specific pipelines (excludes global and modules)
    /// </summary>
    public int HandlerPipelineCount => PipelineCompileModels.Length;

    /// <summary>
    /// True if this handler has no pipelines at all (can use LightHandlerEntry or pure fast path).
    /// </summary>
    public bool IsPipelineFree => TotalPipelineCount == 0;

    /// <summary>
    /// True if this handler has any global pipelines attached.
    /// </summary>
    public bool HasGlobalPipelines => GlobalPipelineCompileModels.Length > 0;

    /// <summary>
    /// True if handler has exactly one handler-specific pipeline and no global pipelines or modules.
    /// Can use SinglePipelineEntry for optimal performance.
    /// </summary>
    public bool HasSinglePipeline => PipelineCompileModels.Length == 1 && 
                                      GlobalPipelineCompileModels.Length == 0 && 
                                      ModuleCompileModels.Length == 0;

    /// <summary>
    /// True if handler has exactly two handler-specific pipelines and no global pipelines or modules.
    /// Can use TwoPipelinesEntry for optimal performance.
    /// </summary>
    public bool HasTwoPipelines => PipelineCompileModels.Length == 2 && 
                                    GlobalPipelineCompileModels.Length == 0 && 
                                    ModuleCompileModels.Length == 0;

    /// <summary>
    /// True if handler has exactly three handler-specific pipelines and no global pipelines or modules.
    /// Can use ThreePipelinesEntry for optimal performance.
    /// </summary>
    public bool HasThreePipelines => PipelineCompileModels.Length == 3 && 
                                      GlobalPipelineCompileModels.Length == 0 && 
                                      ModuleCompileModels.Length == 0;

    /// <summary>
    /// True if handler needs generic registration (4+ pipelines, has modules, or has global pipelines)
    /// </summary>
    public bool NeedsGenericRegistration => TotalPipelineCount > 3 || 
                                            ModuleCompileModels.Length > 0 || 
                                            GlobalPipelineCompileModels.Length > 0;

    public override int GetHashCode() => ToString().GetHashCode();

    public override string ToString() => $"{ClassFullName}.{MethodName}({RequestParameterTypeName}) -> {ReturnTypeName}";
}

