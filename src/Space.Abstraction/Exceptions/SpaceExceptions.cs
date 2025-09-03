using System;
using System.Runtime.Serialization;

namespace Space.Abstraction.Exceptions;

/// <summary>Base exception for Space framework.</summary>
[Serializable]
public abstract class SpaceException : Exception
{
    protected SpaceException() { }
    protected SpaceException(string message) : base(message) { }
    protected SpaceException(string message, Exception inner) : base(message, inner) { }
    protected SpaceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}