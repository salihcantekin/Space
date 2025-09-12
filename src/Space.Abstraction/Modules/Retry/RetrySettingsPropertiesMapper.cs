using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules.Retry;

internal static class RetrySettingsPropertiesMapper
{
    internal static Dictionary<string, object> ToDictionary(IRetrySettingsProperties src)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (src == null)
            return dict;

        dict[nameof(IRetrySettingsProperties.RetryCount)] = src.RetryCount;
        dict[nameof(IRetrySettingsProperties.DelayMilliseconds)] = src.DelayMilliseconds;

        return dict;
    }

    internal static void ApplyTo(IRetrySettingsProperties target, IReadOnlyDictionary<string, object> props)
    {
        if (target == null || props == null)
            return;

        if (props.TryGetValue(nameof(IRetrySettingsProperties.RetryCount), out object rc) && rc != null)
        {
            int val;

            if (rc is int i)
                val = i;
            else
                int.TryParse(rc.ToString(), out val);

            target.RetryCount = val;
        }

        if (props.TryGetValue(nameof(IRetrySettingsProperties.DelayMilliseconds), out object dm) && dm != null)
        {
            int val;

            if (dm is int i2)
                val = i2;
            else
                int.TryParse(dm.ToString(), out val);

            target.DelayMilliseconds = val;
        }
    }
}