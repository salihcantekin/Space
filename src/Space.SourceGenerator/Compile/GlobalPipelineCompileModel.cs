using Space.SourceGenerator.Extensions;
using System;
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

    // Override record's synthesized Equals to only compare key fields for HashSet duplicate detection
    // Two global pipelines are the same if they have the same ClassFullName, MethodName, RequestType, ResponseType, Order, and ExecutionStage
    public virtual bool Equals(GlobalPipelineCompileModel other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // Only compare key identifying fields, not all properties like Properties dictionary
        return ClassFullName == other.ClassFullName &&
               MethodName == other.MethodName &&
               RequestParameterTypeName == other.RequestParameterTypeName &&
               ReturnTypeName == other.ReturnTypeName &&
               Order == other.Order &&
               ExecutionStage == other.ExecutionStage;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + (ClassFullName?.GetHashCode() ?? 0);
            hash = hash * 23 + (MethodName?.GetHashCode() ?? 0);
            hash = hash * 23 + (RequestParameterTypeName?.GetHashCode() ?? 0);
            hash = hash * 23 + (ReturnTypeName?.GetHashCode() ?? 0);
            hash = hash * 23 + Order;
            hash = hash * 23 + ExecutionStage;
            return hash;
        }
    }

    public override string ToString() => $"{ClassFullName}.{MethodName}(GlobalPipeline<{RequestParameterTypeName}, {ReturnTypeName}>, Order={Order}, Stage={ExecutionStage})";
}
