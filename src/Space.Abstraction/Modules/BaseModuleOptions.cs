using Space.Abstraction.Exceptions;
using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules;

public abstract class ProfileModuleOptions<T> : BaseModuleOptions where T : BaseModuleOptions, new()
{
    private readonly Dictionary<string, T> profiles = [];

    public IReadOnlyDictionary<string, T> Profiles => profiles;

    public void WithDefaultProfile(Action<T> configure)
    {
        WithProfile("Default", configure);
    }

    public void WithProfile(string profileName, Action<T> configure)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentException("Profile name cannot be null or whitespace.", nameof(profileName));

        if (configure is null)
            throw new ArgumentNullException(nameof(configure), "Configure action cannot be null.");

        var profileOptions = new T();
        configure(profileOptions);

        profiles[profileName] = profileOptions;
    }
}

/// <summary>
/// Variant for modules that only need per-profile options and do not want provider configuration.
/// </summary>
public abstract class ProfileOnlyModuleOptions<T> where T : class, new()
{
    private readonly Dictionary<string, T> profiles = [];

    public IReadOnlyDictionary<string, T> Profiles => profiles;

    public void WithDefaultProfile(Action<T> configure)
    {
        WithProfile("Default", configure);
    }

    public void WithProfile(string profileName, Action<T> configure)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentException("Profile name cannot be null or whitespace.", nameof(profileName));

        if (configure is null)
            throw new ArgumentNullException(nameof(configure), "Configure action cannot be null.");

        var profileOptions = new T();
        configure(profileOptions);

        profiles[profileName] = profileOptions;
    }
}

public abstract class BaseModuleOptions : IModuleOptions
{
    public IModuleProvider ModuleProvider { get; private set; }
    internal Func<IServiceProvider, IModuleProvider> ModuleProviderAction { get; set; }

    public void WithModuleProvider(IModuleProvider provider)
    {
        if (provider is null)
            throw new ModuleProviderNullException();

        if (ModuleProvider != null)
            throw new ModuleProviderAlreadySetException(ModuleProvider.GetType(), provider.GetType());

        ModuleProvider = provider;
    }

    public void WithModuleProvider(Func<IServiceProvider, IModuleProvider> providerFunc)
    {
        if (providerFunc is null)
            throw new ModuleProviderFactoryNullException();

        if (ModuleProviderAction != null)
            throw new ModuleProviderFactoryAlreadySetException();

        ModuleProviderAction = providerFunc;
    }
}
