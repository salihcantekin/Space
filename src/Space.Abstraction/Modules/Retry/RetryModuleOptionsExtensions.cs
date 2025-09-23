using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Space.Abstraction.Modules.Options;
using System;

namespace Space.Abstraction.Modules.Retry;

/// <summary>
/// Extension methods for registering Retry module with the Options pattern.
/// </summary>
public static class RetryModuleOptionsExtensions
{
    /// <summary>
    /// Registers the Retry module using the standard .NET Options pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action for retry options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpaceRetryOptions(
        this IServiceCollection services,
        Action<RetryOptions> configure = null)
    {
        // Register retry options using the Options pattern
        services.AddSpaceModuleOptions<RetryOptions>(configure);

        // Register the retry module provider if not already registered
        services.TryAddSingleton<IRetryModuleProvider, DefaultRetryModuleProvider>();

        return services;
    }

    /// <summary>
    /// Registers the Retry module using configuration binding.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action that can bind from configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpaceRetryOptionsFromConfiguration(
        this IServiceCollection services,
        Action<RetryOptions> configure)
    {
        // Register retry options using configuration binding
        services.AddSpaceModuleOptionsFromConfiguration<RetryOptions>(configure);

        // Register the retry module provider if not already registered
        services.TryAddSingleton<IRetryModuleProvider, DefaultRetryModuleProvider>();

        return services;
    }

    /// <summary>
    /// Registers the Retry module with a custom provider using the Options pattern.
    /// </summary>
    /// <typeparam name="TProvider">The retry module provider type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action for retry options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpaceRetryOptions<TProvider>(
        this IServiceCollection services,
        Action<RetryOptions> configure = null)
        where TProvider : class, IRetryModuleProvider
    {
        // Register retry options using the Options pattern
        services.AddSpaceModuleOptions<RetryOptions>(configure);

        // Register the custom retry module provider
        services.AddSingleton<IRetryModuleProvider, TProvider>();

        return services;
    }

    /// <summary>
    /// Registers the Retry module with a provider factory using the Options pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="providerFactory">Factory function for creating the retry module provider</param>
    /// <param name="configure">Optional configuration action for retry options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpaceRetryOptions(
        this IServiceCollection services,
        Func<IServiceProvider, IRetryModuleProvider> providerFactory,
        Action<RetryOptions> configure = null)
    {
        if (providerFactory == null)
            throw new ArgumentNullException(nameof(providerFactory));

        // Register retry options using the Options pattern
        services.AddSpaceModuleOptions<RetryOptions>(configure);

        // Register the retry module provider factory
        services.AddSingleton<IRetryModuleProvider>(providerFactory);

        return services;
    }
}
