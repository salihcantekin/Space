namespace Space.Abstraction.Modules.Audit;

/// <summary>
/// Strongly-typed options for the Audit module following the .NET Options pattern.
/// </summary>
public class AuditOptions : IAuditSettingsProperties
{
    /// <summary>
    /// The log level for audit operations.
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Whether to include request details in audit logs.
    /// </summary>
    public bool IncludeRequestDetails { get; set; } = true;

    /// <summary>
    /// Whether to include response details in audit logs.
    /// </summary>
    public bool IncludeResponseDetails { get; set; } = false;

    /// <summary>
    /// Maximum size of logged content in characters.
    /// </summary>
    public int MaxContentLength { get; set; } = 1000;
}
