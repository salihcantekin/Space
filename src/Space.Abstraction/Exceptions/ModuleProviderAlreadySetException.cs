using System;

namespace Space.Abstraction.Exceptions;

[Serializable]
public sealed class ModuleProviderAlreadySetException(Type existing, Type incoming)
    : SpaceException($"Module provider already set to '{existing.FullName}'. Incoming: '{incoming.FullName}'.")
{
    public Type ExistingProviderType { get; } = existing;
    public Type IncomingProviderType { get; } = incoming;
}
