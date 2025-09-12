using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules;

public static class ModuleConfigMerge
{
    public static Dictionary<string, object> Merge(
        IReadOnlyDictionary<string, object> defaultProperties,
        IReadOnlyDictionary<string, object> globalProfileProperties,
        IReadOnlyDictionary<string, object> attributeProperties)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        void AddRange(IReadOnlyDictionary<string, object> src)
        {
            if (src == null) 
                return;
             
            foreach (var kv in src)
            {
                result[kv.Key] = kv.Value;
            }
        }

        AddRange(defaultProperties);          // lowest priority
        AddRange(globalProfileProperties);    // middle priority
        AddRange(attributeProperties);        // highest priority

        return result;
    }
}
