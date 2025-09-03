using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Exceptions;
using Space.Abstraction.Modules;
using Space.Abstraction.Modules.Contracts;
using System;

namespace Space.Modules.InMemoryCache.Cache;

[SpaceModule(ModuleAttributeType = typeof(CacheModuleAttribute), IsEnabled = true)]
public class CacheModule(IServiceProvider serviceProvider) : SpaceModule(serviceProvider)
{
    private IModuleProvider moduleProvider;
    private CacheModuleConfig cacheModuleConfig;

    public override int PipelineOrder => int.MinValue + 2;

    public override Type GetAttributeType()
    {
        return typeof(CacheModuleAttribute);
    }

    public override IModuleConfig GetModuleConfig(HandleIdentifier moduleHandleIdentifier)
    {
        if (cacheModuleConfig is not null)
            return cacheModuleConfig;

        cacheModuleConfig = new();

        var moduleConfig = ServiceProvider.GetKeyedService<ModuleConfig>(moduleHandleIdentifier);
        if (moduleConfig is not null)
        {
            var durationInSec = moduleConfig.GetModuleProperty<int>(nameof(CacheModuleAttribute.Duration));
            cacheModuleConfig.TimeSpan = TimeSpan.FromSeconds(Math.Max(durationInSec, 0));
        }

        return cacheModuleConfig;
    }

    public override IModuleProvider GetDefaultProvider() => default;

    public override IModuleProvider GetModuleProvider()
    {
        if (moduleProvider is not null)
            return moduleProvider;

        moduleProvider = ServiceProvider.GetService<ICacheModuleProvider>() ?? GetDefaultProvider();

        if (moduleProvider is null)
        {
            string message = ModuleProviderFunc is not null
                ? "ModuleProviderFunc returned null."
                : $"No {nameof(CacheModule)}Provider registered in the DI container.";

            throw new ModuleProviderNullException(moduleName: GetModuleName(), message: message);
        }

        return moduleProvider;
    }

    public override ModulePipelineWrapper<TRequest, TResponse> GetModule<TRequest, TResponse>()
    {
        var cacheIdentifier = base.GetModuleKey<TRequest, TResponse>();
        var cachedWrapper = GetWrapper<TRequest, TResponse>(cacheIdentifier);

        if (cachedWrapper is not null)
            return cachedWrapper;

        var cacheModuleProvider = (ICacheModuleProvider)GetModuleProvider();
        var cacheConfig = (CacheModuleConfig)GetModuleConfig(cacheIdentifier);

        CacheModulePipelineWrapper<TRequest, TResponse>.UseCustomCacheProvider(cacheModuleProvider);
        cachedWrapper = new CacheModulePipelineWrapper<TRequest, TResponse>(cacheConfig);

        CacheWrapper(cacheIdentifier, cachedWrapper);
        return cachedWrapper;
    }
}