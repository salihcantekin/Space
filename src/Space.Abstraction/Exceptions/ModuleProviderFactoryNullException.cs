using System;

namespace Space.Abstraction.Exceptions;

[Serializable]
public sealed class ModuleProviderFactoryNullException()
    : SpaceException("Module provider factory delegate cannot be null.")
{
}
