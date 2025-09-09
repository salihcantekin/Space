using System;
using Space.Abstraction.Exceptions;

namespace Space.Abstraction.Modules;

public abstract class BaseModuleOptions : IModuleOptions
{
    public IModuleProvider ModuleProvider { get; private set; }
    internal Func<IServiceProvider, IModuleProvider> ModuleProviderAction { get; set; }
    public string ProfileName { get; set; } = "Default";

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

    public void WithProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentException("Profile name cannot be null or empty", nameof(profileName));

        ProfileName = profileName;
    }
}
