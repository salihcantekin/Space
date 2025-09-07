using System;

namespace Space.Abstraction.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class HandleAttribute : Attribute
{
    public string Name { get; set; } = Guid.NewGuid().ToString(); // Unique identifier for the handler

    // When true, this handler becomes the default selection for its (Request, Response) pair
    // if Send is called without an explicit name.
    public bool IsDefault { get; set; }

    public HandleAttribute() { }
}