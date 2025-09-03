using System;

namespace Space.Abstraction.Exceptions;

[Serializable]
public sealed class SpaceGeneratedInvocationException(string methodName, Exception inner) 
    : SpaceException($"Generated method '{methodName}' invocation failed.", inner)
{
    public string MethodName { get; } = methodName;
}
