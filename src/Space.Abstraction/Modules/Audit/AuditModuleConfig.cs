namespace Space.Abstraction.Modules.Audit;

public class AuditModuleConfig : IModuleConfig, IAuditSettingsProperties
{
    public string LogLevel { get; set; }
}