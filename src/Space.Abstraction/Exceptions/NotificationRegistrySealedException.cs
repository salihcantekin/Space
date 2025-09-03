using System;

namespace Space.Abstraction.Exceptions;

[Serializable]
public sealed class NotificationRegistrySealedException()
    : SpaceException("Notification registration sealed; no further registrations allowed.")
{
}
