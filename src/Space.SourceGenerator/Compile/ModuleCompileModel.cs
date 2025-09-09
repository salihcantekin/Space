using System.Collections.Generic;

namespace Space.SourceGenerator.Compile;

public record ModuleProviderCompileModel(string ModuleName);

public class ModuleCompileModel
{
    private string classFullName;
    public string ClassFullName
    {
        get => classFullName;
        set => classFullName = value;
    }

    public string MethodName { get; set; }
    public string RequestType { get; set; }
    public string ResponseType { get; set; }
    public string ModuleName { get; set; }
    public string ProfileName { get; set; } = "Default";

    public string ModuleProviderType { get; set; }

    public Dictionary<string, object> ModuleProperties { get; set; } = [];
}
