using System;
using System.Collections.Generic;
using System.ComponentModel;

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
        if (value == null)
            return default;

        TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));

        var valueType = value?.GetType() ?? typeof(object);
        if (!converter.CanConvertFrom(valueType))
            return (T)value;
        return (T)converter.ConvertFrom(value);
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

    public IReadOnlyDictionary<string, object> GetAllModuleProperties()
    {
        if (Properties.TryGetValue(ModulePropertiesKey, out var dictObj) && dictObj is Dictionary<string, object> dict)
            return dict;

        return new Dictionary<string, object>();
    }

    public override int GetHashCode()
    {
        var classFullName = GetProperty("ClassFullName")?.ToString() ?? string.Empty;
        var methodName = GetProperty("MethodName")?.ToString() ?? string.Empty;

        return ($"{classFullName}.{methodName}.{ModuleName}").GetHashCode();
    }
}
