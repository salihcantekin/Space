using System;

namespace Space.Abstraction.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class HandleAttribute : Attribute
{
    public string Name { get; set; } = Guid.NewGuid().ToString(); // Unique identifier for the handler

    public HandleAttribute() { }
}