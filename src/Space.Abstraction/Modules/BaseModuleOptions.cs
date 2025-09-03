using System;
using Space.Abstraction.Exceptions;

namespace Space.Abstraction.Modules;

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
