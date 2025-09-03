using System;

namespace Space.Abstraction.Exceptions;

[Serializable]
public sealed class SpaceModuleRegistrationMissisng(string moduleAttribute)
        : SpaceException($"Space module hasn't been registed. Please check Source Generator. Attribute: {moduleAttribute}")
{
    public string ModuleAttribute { get; } = moduleAttribute;
}