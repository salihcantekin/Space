using System;

namespace Space.Abstraction.Modules.Cache;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class CacheModuleAttribute : Attribute, ISpaceModuleAttribute
{
    public string ProfileName { get; set; } = "Default";
    public string Duration { get; set; }
    public string Provider { get; set; }
    public string MaxSize { get; set; }
}