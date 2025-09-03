using System;

namespace Space.Abstraction.Modules;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class SpaceModuleAttribute : Attribute
{
    public Type ModuleAttributeType { get; set; }

    public bool IsEnabled { get; set; }
}
