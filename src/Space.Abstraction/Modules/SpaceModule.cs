using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules;

public abstract class SpaceModule(IServiceProvider serviceProvider)
{
    private readonly Dictionary<ModuleIdentifier, object> moduleWrappers = [];
    private readonly Dictionary<ModuleIdentifier, object> moduleConfigs = [];
    private readonly object wrappersLock = new();
    private readonly object configsLock = new();

    protected IServiceProvider ServiceProvider { get; } = serviceProvider;

    protected Func<IServiceProvider, IModuleProvider> ModuleProviderFunc { get; set; }

    protected void CacheWrapper<TRequest, TResponse>(ModuleIdentifier wrapperIdentifier, ModulePipelineWrapper<TRequest, TResponse> moduleWrapper)
    {
        lock (wrappersLock)
        {
            moduleWrappers[wrapperIdentifier] = moduleWrapper;
        }
    }

    protected ModulePipelineWrapper<TRequest, TResponse> GetWrapper<TRequest, TResponse>(ModuleIdentifier wrapperIdentifier)
    {
        lock (wrappersLock)
        {
            return moduleWrappers.TryGetValue(wrapperIdentifier, out var wrapper)
                ? wrapper as ModulePipelineWrapper<TRequest, TResponse>
                : default;
        }
    }

    protected TConfig GetOrAddConfig<TConfig>(ModuleIdentifier id, Func<TConfig> factory)
        where TConfig : class, IModuleConfig
    {
        if (factory is null)
            throw new ArgumentNullException(nameof(factory));

        lock (configsLock)
        {
            if (moduleConfigs.TryGetValue(id, out var cfg) && cfg is TConfig typed)
                return typed;

            var created = factory();
            moduleConfigs[id] = created;
            return created;
        }
    }

    public void SetOptionAction(Func<IServiceProvider, IModuleProvider> moduleProviderFunc)
    {
        ModuleProviderFunc = moduleProviderFunc;
    }

    public virtual Type GetAttributeType() => default;

    public virtual IModuleProvider GetModuleProvider() => default;

    public abstract int PipelineOrder { get; }

    public virtual IModuleProvider GetDefaultProvider() => default;

    public virtual IModuleConfig GetModuleConfig(ModuleIdentifier moduleHandleIdentifier)
    {
        var moduleConfig = ServiceProvider.GetKeyedService<ModuleConfig>(moduleHandleIdentifier);
        return moduleConfig;
    }

    public virtual string GetModuleName() => GetAttributeType().Name;

    public virtual ModuleIdentifier GetModuleKey<TRequest, TResponse>(string profileName)
    {
        var identifier = ModuleIdentifier.From<TRequest, TResponse>(GetModuleName(), profileName);
        return identifier;
    }

    public abstract ModulePipelineWrapper<TRequest, TResponse> GetModule<TRequest, TResponse>(string profileName)
        where TRequest : notnull
        where TResponse : notnull;
}