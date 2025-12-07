using Space.SourceGenerator.Extensions;
using System.Collections.Generic;

namespace Space.SourceGenerator.Compile;

/// <summary>
/// Represents a global pipeline that applies to all handlers with matching TRequest and TResponse.
/// </summary>
public record GlobalPipelineCompileModel : BaseCompileModel
{
    /// <summary>
    /// Order within the execution stage (lower executes first)
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Execution stage relative to handler-specific pipelines
    /// </summary>
    public int ExecutionStage { get; set; }

    /// <summary>
    /// Indicates whether the pipeline method is generic (has TRequest, TResponse type parameters)
    /// </summary>
    public bool IsGeneric { get; set; }

    public bool IsTask => ReturnTaskTypeName.IsTask();

    public bool IsValueTask => ReturnTaskTypeName.IsValueTask();

    public Dictionary<string, object> Properties { get; set; } = [];

    public override int GetHashCode() => ToString().GetHashCode();

    public override string ToString() => $"{ClassFullName}.{MethodName}(GlobalPipeline<{RequestParameterTypeName}, {ReturnTypeName}>)";
}
