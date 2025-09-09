using System;

namespace Space.Abstraction.Modules.Audit;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class AuditModuleAttribute : Attribute, ISpaceModuleAttribute
{
    public string ProfileName { get; set; } = "Default";
    public string LogLevel { get; set; }
    public string IncludeStackTrace { get; set; }
    public string MaxLogSize { get; set; }
}