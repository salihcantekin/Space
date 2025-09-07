using System;

namespace Space.Abstraction.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class PipelineAttribute(string handleName = null) : Attribute, IPipelineConfig
{
    public int Order { get; set; } = 100; // Default order for pipeline modules

    public string HandleName { get; } = handleName;
}


public interface IPipelineConfig
{
    int Order { get; set; }
}