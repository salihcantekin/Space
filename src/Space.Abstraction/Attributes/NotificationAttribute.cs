using System;

namespace Space.Abstraction.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class NotificationAttribute : Attribute
{
}