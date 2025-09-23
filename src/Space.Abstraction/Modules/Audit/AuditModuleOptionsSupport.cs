using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Space.Abstraction.Modules.Options;
using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules.Audit;

/// <summary>
/// Extension methods and utilities for AuditModule to support both legacy and Options pattern approaches.
/// </summary>
public static class AuditModuleOptionsSupport
{
    /// <summary>
    /// Gets audit options using the new Options pattern with fallback to legacy approach.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="moduleIdentifier">The module identifier for attribute overrides</param>
    /// <returns>The resolved audit options</returns>
    public static AuditOptions GetAuditOptions(this IServiceProvider serviceProvider, ModuleIdentifier moduleIdentifier)
    {
        // Try new Options pattern first
        var optionsProvider = serviceProvider.GetService<IModuleOptionsProvider<AuditOptions>>();
        if (optionsProvider != null)
        {
            return optionsProvider.GetOptions(moduleIdentifier);
        }

        // Fallback to legacy approach
        return GetLegacyAuditOptions(serviceProvider, moduleIdentifier);
    }

    /// <summary>
    /// Gets audit configuration using the legacy approach for backward compatibility.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="moduleIdentifier">The module identifier</param>
    /// <returns>The resolved audit configuration</returns>
    public static AuditModuleConfig GetLegacyAuditConfig(this IServiceProvider serviceProvider, ModuleIdentifier moduleIdentifier)
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

        var cfg = new AuditModuleConfig();
        AuditSettingsPropertiesMapper.ApplyTo(cfg, merged);
        return cfg;
    }

    private static AuditOptions GetLegacyAuditOptions(IServiceProvider serviceProvider, ModuleIdentifier moduleIdentifier)
    {
        var legacyConfig = GetLegacyAuditConfig(serviceProvider, moduleIdentifier);
        
        // Convert legacy config to new options
        return new AuditOptions
        {
            LogLevel = legacyConfig.LogLevel ?? "Information"
        };
    }

    private static IReadOnlyDictionary<string, AuditModuleOptions> GetLegacyGlobalProfiles(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<IModuleGlobalOptionsAccessor<AuditModuleOptions>>()?.Profiles
               ?? new Dictionary<string, AuditModuleOptions>();
    }

    private static IReadOnlyDictionary<string, object> ExtractProfileProperties(AuditModuleOptions profileOpt)
    {
        if (profileOpt is null)
            return new Dictionary<string, object>();

        return AuditSettingsPropertiesMapper.ToDictionary(profileOpt);
    }

    private static string NormalizeProfileName(string name)
        => string.IsNullOrWhiteSpace(name) ? ModuleConstants.DefaultProfileName : name;
}
