using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules;


public class ModuleConfig(string moduleName) : BaseConfig, IModuleConfig
{
    public const string ModulePropertiesKey = "ModuleProperties";

    public string ModuleName { get; set; } = moduleName;
    public string ProfileName { get; set; } = string.Empty;
    public Dictionary<string, object> ProfileDefaults { get; set; } = [];

    public object GetModuleProperty(string key)
    {
        // First try to get from module-specific properties
        if (Properties.TryGetValue(ModulePropertiesKey, out var dictObj) && dictObj is Dictionary<string, object> dict)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                return value;
            }
        }

        // Fallback to profile defaults if property not found or null
        if (ProfileDefaults.TryGetValue(key, out var defaultValue))
        {
            return defaultValue;
        }

        return null;
    }

    public T GetModuleProperty<T>(string key)
    {
        var value = GetModuleProperty(key);

        if (value == null)
        {
            return default;
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }

    public void SetModuleProperties(Dictionary<string, object> properties)
    {
        Properties[ModulePropertiesKey] = properties;
    }

    public void SetModuleProperty(string key, object value)
    {
        if (!Properties.TryGetValue(ModulePropertiesKey, out var dictObj) || dictObj is not Dictionary<string, object> dict)
        {
            dict = [];
            Properties[ModulePropertiesKey] = dict;
        }

        dict[key] = value;
    }

    public void SetProfileDefaults(Dictionary<string, object> defaults)
    {
        ProfileDefaults = defaults ?? [];
    }

    public override int GetHashCode()
    {
        return $"{GetProperty("ClassFullName")}.{GetProperty("MethodName")}.{ModuleName}.{ProfileName}".GetHashCode();
    }
}
