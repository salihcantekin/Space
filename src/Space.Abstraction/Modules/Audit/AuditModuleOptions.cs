using System;

namespace Space.Abstraction.Modules.Audit;

/// <summary>
/// Legacy audit module options class for backward compatibility.
/// 
/// ⚠️ DEPRECATED: This class is deprecated. Use AuditOptions with the new Options pattern instead.
/// 
/// Migration example:
/// OLD: services.AddSpaceAudit(opt => opt.WithDefaultProfile(p => p.LogLevel = "Debug"));
/// NEW: services.AddSpaceAuditOptions(opt => opt.LogLevel = "Debug");
/// </summary>
[Obsolete("Use AuditOptions with AddSpaceAuditOptions() instead. This class will be removed in a future version.")]
public class AuditModuleOptions : ProfileModuleOptions<AuditModuleOptions>, IAuditSettingsProperties
{
    public string LogLevel { get; set; }
}
