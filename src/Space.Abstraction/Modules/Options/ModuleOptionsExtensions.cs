using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Space.Abstraction.Modules.Options;

/// <summary>
/// Extension methods for working with module options in Space modules.
/// </summary>
public static class ModuleOptionsExtensions
{
    /// <summary>
    /// Gets strongly-typed options for a module using the Options pattern.
    /// </summary>
    /// <typeparam name="TOptions">The options type</typeparam>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="moduleIdentifier">The module identifier for attribute overrides</param>
    /// <returns>The resolved options instance</returns>
    public static TOptions GetModuleOptions<TOptions>(
        this IServiceProvider serviceProvider,
        ModuleIdentifier moduleIdentifier)
        where TOptions : class, new()
    {
        var optionsProvider = serviceProvider.GetService<IModuleOptionsProvider<TOptions>>();
        if (optionsProvider != null)
        {
            return optionsProvider.GetOptions(moduleIdentifier);
        }

        // Fallback to standard IOptions if no custom provider is registered
        var options = serviceProvider.GetService<IOptions<TOptions>>();
        return options?.Value ?? new TOptions();
    }

    /// <summary>
    /// Gets strongly-typed options for a module using the Options pattern with profile support.
    /// </summary>
    /// <typeparam name="TOptions">The options type</typeparam>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="profileName">The profile name (defaults to "Default")</param>
    /// <returns>The resolved options instance</returns>
    public static TOptions GetModuleOptions<TOptions>(
        this IServiceProvider serviceProvider,
        string profileName = null)
        where TOptions : class, new()
    {
        var optionsProvider = serviceProvider.GetService<IModuleOptionsProvider<TOptions>>();
        if (optionsProvider != null)
        {
            return optionsProvider.GetOptions(profileName);
        }

        // Fallback to standard IOptions if no custom provider is registered
        var options = serviceProvider.GetService<IOptions<TOptions>>();
        return options?.Value ?? new TOptions();
    }
}
