using System;

namespace Space.Abstraction.Exceptions;

[Serializable]
public sealed class SpaceGeneratedMethodNotFoundException(string methodName, string expectedType) 
    : SpaceException($"Generated method '{methodName}' not found. Expected type: {expectedType}.")
{
    public string MethodName { get; } = methodName;
    public string ExpectedType { get; } = expectedType;
}
