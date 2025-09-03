using System;

namespace Space.Abstraction.Exceptions;

[Serializable]
public sealed class SpaceAssemblyLoadException(string message) 
    : SpaceException(message)
{
}
