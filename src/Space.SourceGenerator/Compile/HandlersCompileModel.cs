using Space.SourceGenerator.Extensions;

namespace Space.SourceGenerator.Compile;

public record HandlersCompileModel(string HandlerName) : BaseCompileModel
{
    public PipelineCompileModel[] PipelineCompileModels { get; set; } = [];

    public ModuleCompileModel[] ModuleCompileModels { get; set; } = [];

    // New: mark handler as default for its Req/Res pair
    public bool IsDefault { get; set; }

    public bool IsReturnTypeEmpty => string.IsNullOrEmpty(ReturnTypeName);
    public bool IsTask => ReturnTaskTypeName.IsTask();
    public bool IsValueTask => ReturnTaskTypeName.IsValueTask();

    public override int GetHashCode() => ToString().GetHashCode();

    public override string ToString() => $"{ClassFullName}.{MethodName}({RequestParameterTypeName}) -> {ReturnTypeName}";
}

