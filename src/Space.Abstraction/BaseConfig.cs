using System.Collections.Generic;

namespace Space.Abstraction;

public class BaseConfig
{
    public Dictionary<string, object> Properties { get; set; } = [];

    public object GetProperty(string key)
    {
        return Properties.TryGetValue(key, out var value) ? value : null;
    }

    public void SetProperty(string key, object value)
    {
        Properties[key] = value;
    }
}

public class PipelineConfig(int order) : IPipelineConfig
{
    public int Order { get; set; } = order;

    public PipelineConfig() : this(0)
    {

    }
}