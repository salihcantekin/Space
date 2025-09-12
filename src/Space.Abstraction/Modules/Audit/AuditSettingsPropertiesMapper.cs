using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules.Audit;

internal static class AuditSettingsPropertiesMapper
{
    internal static Dictionary<string, object> ToDictionary(IAuditSettingsProperties src)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (src == null) 
            return dict;

        if (src.LogLevel != null) 
            dict[nameof(IAuditSettingsProperties.LogLevel)] = src.LogLevel;

        return dict;
    }

    internal static void ApplyTo(IAuditSettingsProperties target, IReadOnlyDictionary<string, object> props)
    {
        if (target == null || props == null) 
            return;

        if (props.TryGetValue(nameof(IAuditSettingsProperties.LogLevel), out object lvl) && lvl != null)
            target.LogLevel = lvl.ToString();
    }
}