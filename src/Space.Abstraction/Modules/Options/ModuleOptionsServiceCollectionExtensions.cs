using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Space.Abstraction.Modules.Options;

/// <summary>
/// Extension methods for registering module options using the standard .NET Options pattern.
/// </summary>
public static class ModuleOptionsServiceCollectionExtensions
{
    /// <summary>
    /// Registers module options using the standard .NET Options pattern with Space-specific enhancements.
    /// </summary>
    /// <typeparam name="TOptions">The strongly-typed options class</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action for the options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpaceModuleOptions<TOptions>(
        this IServiceCollection services,
        Action<TOptions> configure = null)
        where TOptions : class, new()
    {
        // Register with standard .NET Options pattern
        if (configure != null)
        {
            services.Configure<TOptions>(configure);
        }
        else
        {
            services.Configure<TOptions>(_ => { });
        }

        // Register our custom module options provider
        services.AddSingleton<IModuleOptionsProvider<TOptions>, ModuleOptionsProvider<TOptions>>();

        return services;
    }

    /// <summary>
    /// Registers module options using the standard .NET Options pattern with configuration section binding.
    /// Note: For full configuration binding support, consider using Microsoft.Extensions.Configuration.Binder package.
    /// </summary>
    /// <typeparam name="TOptions">The strongly-typed options class</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action that can bind from configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpaceModuleOptionsFromConfiguration<TOptions>(
        this IServiceCollection services,
        Action<TOptions> configure)
        where TOptions : class, new()
    {
        // Register with standard .NET Options pattern
        services.Configure<TOptions>(configure);

        // Register our custom module options provider
        services.AddSingleton<IModuleOptionsProvider<TOptions>, ModuleOptionsProvider<TOptions>>();

        return services;
    }

    /// <summary>
    /// Registers module options with profile support and legacy compatibility.
    /// </summary>
    /// <typeparam name="TOptions">The strongly-typed options class that extends BaseModuleOptions</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action for profile-based options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpaceModuleOptionsWithProfiles<TOptions>(
        this IServiceCollection services,
        Action<ProfileModuleOptions<TOptions>> configure = null)
        where TOptions : BaseModuleOptions, new()
    {
        // Create a concrete implementation of ProfileModuleOptions
        var profileOptions = new ConcreteProfileModuleOptions<TOptions>();
        configure?.Invoke(profileOptions);

        // Register profile accessor for backward compatibility
        services.AddSingleton<IModuleGlobalOptionsAccessor<TOptions>>(
            sp => new ModuleGlobalOptionsAccessor<TOptions>(profileOptions.Profiles));

        // Register with standard .NET Options pattern (use default profile as base)
        services.Configure<TOptions>(options =>
        {
            if (profileOptions.Profiles.TryGetValue("Default", out var defaultProfile))
            {
                // Copy properties from default profile to base options
                CopyProperties(defaultProfile, options);
            }
        });

        // Register our custom module options provider
        services.AddSingleton<IModuleOptionsProvider<TOptions>, ModuleOptionsProvider<TOptions>>();

        return services;
    }

    private static void CopyProperties<T>(T source, T target) where T : class
    {
        if (source == null || target == null) return;

        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            if (property.CanRead && property.CanWrite)
            {
                var value = property.GetValue(source);
                if (value != null)
                {
                    property.SetValue(target, value);
                }
            }
        }
    }
}
