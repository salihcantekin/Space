using System;

namespace Space.Abstraction.Exceptions;

[Serializable]
public sealed class SpaceModuleAttributeInvalidException(Type moduleType, string reason)
        : SpaceException($"Space module '{moduleType.FullName}' has invalid attribute: {reason}")
{
    public Type ModuleType { get; } = moduleType;
}
