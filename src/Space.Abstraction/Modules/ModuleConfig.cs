using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules;


public class ModuleConfig(string moduleName) : BaseConfig, IModuleConfig
{
    public const string ModulePropertiesKey = "ModuleProperties";

    public string ModuleName { get; set; } = moduleName;


    public object GetModuleProperty(string key)
    {
        if (Properties.TryGetValue(ModulePropertiesKey, out var dictObj) && dictObj is Dictionary<string, object> dict)
        {
            return dict.TryGetValue(key, out var value) ? value : null;
        }

        return null;
    }

    public T GetModuleProperty<T>(string key)
    {
        var value = GetModuleProperty(key);
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

    public override int GetHashCode()
    {
        return $"{GetProperty("ClassFullName")}.{GetProperty("MethodName")}.{ModuleName}".GetHashCode();
    }
}
