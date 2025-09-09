using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System;

namespace Space.Abstraction;

public partial class SpaceOptions
{
    private readonly List<string> moduleProviderAttributes = [];
    private readonly Dictionary<string, Dictionary<string, object>> moduleProfiles = [];

    public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Scoped;

    public NotificationDispatchType NotificationDispatchType { get; set; } = NotificationDispatchType.Sequential;

    /// <summary>
    /// Configures global default values for a module profile
    /// </summary>
    public SpaceOptions ConfigureModuleProfile(string moduleName, string profileName, Action<Dictionary<string, object>> configure)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            throw new ArgumentException("Module name cannot be null or empty", nameof(moduleName));
        
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentException("Profile name cannot be null or empty", nameof(profileName));

        var key = $"{moduleName}:{profileName}";
        if (!moduleProfiles.ContainsKey(key))
        {
            moduleProfiles[key] = [];
        }

        configure?.Invoke(moduleProfiles[key]);
        return this;
    }

    /// <summary>
    /// Gets global default configuration for a module profile
    /// </summary>
    public Dictionary<string, object> GetModuleProfileConfiguration(string moduleName, string profileName)
    {
        if (string.IsNullOrWhiteSpace(moduleName) || string.IsNullOrWhiteSpace(profileName))
            return [];

        var key = $"{moduleName}:{profileName}";
        return moduleProfiles.TryGetValue(key, out var config) ? config : [];
    }
}

public enum NotificationDispatchType
{
    Sequential = 0,
    Parallel = 1
}