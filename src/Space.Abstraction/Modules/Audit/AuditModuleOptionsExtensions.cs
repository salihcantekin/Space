using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Space.Abstraction.Modules.Options;
using System;

namespace Space.Abstraction.Modules.Audit;

/// <summary>
/// Extension methods for registering Audit module with the Options pattern.
/// </summary>
public static class AuditModuleOptionsExtensions
{
    /// <summary>
    /// Registers the Audit module using the standard .NET Options pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action for audit options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpaceAuditOptions(
        this IServiceCollection services,
        Action<AuditOptions> configure = null)
    {
        // Register audit options using the Options pattern
        services.AddSpaceModuleOptions<AuditOptions>(configure);

        // Register the audit module provider if not already registered
        services.TryAddSingleton<IAuditModuleProvider, NullAuditModuleProvider>();

        return services;
    }

    /// <summary>
    /// Registers the Audit module using configuration binding.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action that can bind from configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpaceAuditOptionsFromConfiguration(
        this IServiceCollection services,
        Action<AuditOptions> configure)
    {
        // Register audit options using configuration binding
        services.AddSpaceModuleOptionsFromConfiguration<AuditOptions>(configure);

        // Register the audit module provider if not already registered
        services.TryAddSingleton<IAuditModuleProvider, NullAuditModuleProvider>();

        return services;
    }

    /// <summary>
    /// Registers the Audit module with a custom provider using the Options pattern.
    /// </summary>
    /// <typeparam name="TProvider">The audit module provider type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action for audit options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpaceAuditOptions<TProvider>(
        this IServiceCollection services,
        Action<AuditOptions> configure = null)
        where TProvider : class, IAuditModuleProvider
    {
        // Register audit options using the Options pattern
        services.AddSpaceModuleOptions<AuditOptions>(configure);

        // Register the custom audit module provider
        services.AddSingleton<IAuditModuleProvider, TProvider>();

        return services;
    }

    /// <summary>
    /// Registers the Audit module with a provider factory using the Options pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="providerFactory">Factory function for creating the audit module provider</param>
    /// <param name="configure">Optional configuration action for audit options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpaceAuditOptions(
        this IServiceCollection services,
        Func<IServiceProvider, IAuditModuleProvider> providerFactory,
        Action<AuditOptions> configure = null)
    {
        if (providerFactory == null)
            throw new ArgumentNullException(nameof(providerFactory));

        // Register audit options using the Options pattern
        services.AddSpaceModuleOptions<AuditOptions>(configure);

        // Register the audit module provider factory
        services.AddSingleton<IAuditModuleProvider>(providerFactory);

        return services;
    }
}
