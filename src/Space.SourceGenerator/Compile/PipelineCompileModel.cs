using Space.SourceGenerator.Extensions;
using System.Collections.Generic;

namespace Space.SourceGenerator.Compile;


public record PipelineCompileModel : BaseCompileModel
{
    public string HandlerName { get; set; }

    public int Order { get; set; }

    public bool IsTask => ReturnTaskTypeName.IsTask();

    public bool IsValueTask => ReturnTaskTypeName.IsValueTask();

    public Dictionary<string, object> Properties { get; set; } = [];

    public override int GetHashCode() => ToString().GetHashCode();

    public override string ToString() => $"{ClassFullName}.{MethodName}({HandlerName})";
}

