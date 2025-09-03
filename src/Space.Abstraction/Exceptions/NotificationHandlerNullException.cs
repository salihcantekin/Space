using System;

namespace Space.Abstraction.Exceptions;

[Serializable]
public sealed class NotificationHandlerNullException(Type requestType, string name) 
    : SpaceException($"Notification handler delegate is null for request '{requestType.FullName}' (name='{name}').")
{
    public Type RequestType { get; } = requestType; public string Name { get; } = name;
}
