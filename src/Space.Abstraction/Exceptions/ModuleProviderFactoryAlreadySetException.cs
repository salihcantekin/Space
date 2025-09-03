using System;

namespace Space.Abstraction.Exceptions;

[Serializable]
public sealed class ModuleProviderFactoryAlreadySetException()
    : SpaceException("Module provider factory already set.")
{
}
