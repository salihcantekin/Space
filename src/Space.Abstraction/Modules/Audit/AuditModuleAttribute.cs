using System;

namespace Space.Abstraction.Modules.Audit;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class AuditModuleAttribute : Attribute, ISpaceModuleAttribute
{
    public string Profile { get; set; } = "Default";

    public string LogLevel { get; set; } = "Information";

    public Type Provider { get; set; }

    public AuditModuleAttribute() { }

    public AuditModuleAttribute(string profileName)
    {
        Profile = profileName;
    }

    public AuditModuleAttribute(string profileName, string logLevel)
    {
        Profile = profileName;
        LogLevel = logLevel;
    }
}