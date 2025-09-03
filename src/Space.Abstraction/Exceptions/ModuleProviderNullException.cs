using System;

namespace Space.Abstraction.Exceptions;

[Serializable]
public sealed class ModuleProviderNullException: SpaceException
{
    public string ModuleName { get; }

    public ModuleProviderNullException(): base("No module provider found")
    {
        
    }

    public ModuleProviderNullException(string moduleName)
        : base($"Module provider is null for module: {moduleName}")
    {
        ModuleName = moduleName;
    }

    public ModuleProviderNullException(string moduleName, string message)
        : base(message)
    {
        ModuleName = moduleName;
    }
}
