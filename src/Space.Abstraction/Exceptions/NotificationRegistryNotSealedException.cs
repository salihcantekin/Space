using System;

namespace Space.Abstraction.Exceptions;

[Serializable]
public sealed class NotificationRegistryNotSealedException()
    : SpaceException("Notification registration not sealed. Call CompleteRegistration() first.")
{ }
