using System;
using Space.Abstraction.Exceptions;

namespace Space.Abstraction.Modules;

/*
 
    # Source Generator
    - We need to know what module attributes are used so we can add these to DI for the other services to use
    - We also need to add the properties so we can generate [Cache]ModuleConfig from it


    # We need a mapping where Attributes are mapped to Module Components
    - ModuleProvider
    - ModuleConfig
    - ModuleAttribute
    

    # We also need users to be able to customize ModuleConfig(Attribute properties) and ModuleProvider (cache provider)
    via Extension functions

    # In Space module, we'll receive TRequest, TResponse and ModuleName
    - We'll need to know;
      - What module attribute it is
      - What module provider to use
      - What module config to use

    # We need ModulePipelineWrapper to handle the base logic of the modules
    - For instance, CacheModulePipelineWrapper will handle the caching logic
 
 
 */





//public static class CacheModuleConfigExtension
//{
//    public static CacheModuleConfig ToCacheConfig(this ModuleConfig config)
//    {
//        CacheModuleConfig cacheConfig = new();

//        var durationInSec = config.GetModuleProperty<int>("Duration");
//        cacheConfig.TimeSpan = TimeSpan.FromSeconds(durationInSec);

//        return cacheConfig;
//    }
//}



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
