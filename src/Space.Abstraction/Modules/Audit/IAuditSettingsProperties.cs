namespace Space.Abstraction.Modules.Audit;

/// <summary>
/// Common settings shared by Audit attribute/options/config.
/// </summary>
public interface IAuditSettingsProperties
{
    string LogLevel { get; set; }
}
