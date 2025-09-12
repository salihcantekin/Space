using System;

namespace Space.Abstraction.Modules.Audit;

public class AuditModuleOptions : ProfileModuleOptions<AuditModuleOptions>, IAuditSettingsProperties
{
    public string LogLevel { get; set; }
}
