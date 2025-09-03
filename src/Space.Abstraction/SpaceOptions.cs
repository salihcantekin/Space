using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Space.Abstraction;

public partial class SpaceOptions
{
    private readonly List<string> moduleProviderAttributes = [];

    public List<string> Modules => moduleProviderAttributes;

    public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Scoped;

    public NotificationDispatchType NotificationDispatchType { get; set; } = NotificationDispatchType.Sequential;

}

public enum NotificationDispatchType
{
    Sequential = 0,
    Parallel = 1
}