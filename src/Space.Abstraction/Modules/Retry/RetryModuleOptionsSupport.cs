using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Space.Abstraction.Modules.Options;
using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules.Retry;

/// <summary>
/// Extension methods and utilities for RetryModule to support both legacy and Options pattern approaches.
/// </summary>
public static class RetryModuleOptionsSupport
{
    /// <summary>
    /// Gets retry options using the new Options pattern with fallback to legacy approach.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="moduleIdentifier">The module identifier for attribute overrides</param>
    /// <returns>The resolved retry options</returns>
    public static RetryOptions GetRetryOptions(this IServiceProvider serviceProvider, ModuleIdentifier moduleIdentifier)
    {
        // Try new Options pattern first
        var optionsProvider = serviceProvider.GetService<IModuleOptionsProvider<RetryOptions>>();
        if (optionsProvider != null)
        {
            return optionsProvider.GetOptions(moduleIdentifier);
        }

        // Fallback to legacy approach
        return GetLegacyRetryOptions(serviceProvider, moduleIdentifier);
    }

    /// <summary>
    /// Gets retry configuration using the legacy approach for backward compatibility.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="moduleIdentifier">The module identifier</param>
    /// <returns>The resolved retry configuration</returns>
    public static RetryModuleConfig GetLegacyRetryConfig(this IServiceProvider serviceProvider, ModuleIdentifier moduleIdentifier)
    {
        IReadOnlyDictionary<string, object> defaultProps = new Dictionary<string, object>();

        var globalProfiles = GetLegacyGlobalProfiles(serviceProvider);
        var requested = NormalizeProfileName(moduleIdentifier.ProfileName);
        if (!globalProfiles.TryGetValue(requested, out var profileOpt))
        {
            globalProfiles.TryGetValue(ModuleConstants.DefaultProfileName, out profileOpt);
        }
        var globalProfileProps = ExtractProfileProperties(profileOpt);

        var attributeConfig = serviceProvider.GetKeyedService<ModuleConfig>(moduleIdentifier);
        var attributeProps = attributeConfig?.GetAllModuleProperties();

        var merged = ModuleConfigMerge.Merge(defaultProps, globalProfileProps, attributeProps);

        var cfg = new RetryModuleConfig();
        RetrySettingsPropertiesMapper.ApplyTo(cfg, merged);
        return cfg;
    }

    private static RetryOptions GetLegacyRetryOptions(IServiceProvider serviceProvider, ModuleIdentifier moduleIdentifier)
    {
        var legacyConfig = GetLegacyRetryConfig(serviceProvider, moduleIdentifier);
        
        // Convert legacy config to new options
        return new RetryOptions
        {
            RetryCount = legacyConfig.RetryCount,
            DelayMilliseconds = legacyConfig.DelayMilliseconds
        };
    }

    private static IReadOnlyDictionary<string, RetryModuleOptions> GetLegacyGlobalProfiles(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<IModuleGlobalOptionsAccessor<RetryModuleOptions>>()?.Profiles
               ?? new Dictionary<string, RetryModuleOptions>();
    }

    private static IReadOnlyDictionary<string, object> ExtractProfileProperties(RetryModuleOptions profileOpt)
    {
        return profileOpt is null
                ? new Dictionary<string, object>()
                : RetrySettingsPropertiesMapper.ToDictionary(profileOpt);
    }

    private static string NormalizeProfileName(string name)
        => string.IsNullOrWhiteSpace(name) ? ModuleConstants.DefaultProfileName : name;
}
