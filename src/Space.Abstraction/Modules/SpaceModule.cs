using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules;


public abstract class SpaceModule(IServiceProvider serviceProvider)
{
    private readonly Dictionary<HandleIdentifier, object> moduleWrappers = [];

    protected IServiceProvider ServiceProvider { get; } = serviceProvider;

    protected Func<IServiceProvider, IModuleProvider> ModuleProviderFunc { get; set; }

    protected void CacheWrapper<TRequest, TResponse>(HandleIdentifier wrapperIdentifier, ModulePipelineWrapper<TRequest, TResponse> moduleWrapper)
    {
        moduleWrappers[wrapperIdentifier] = moduleWrapper;
    }

    protected ModulePipelineWrapper<TRequest, TResponse> GetWrapper<TRequest, TResponse>(HandleIdentifier wrapperIdentifier)
    {
        return moduleWrappers.TryGetValue(wrapperIdentifier, out var wrapper)
            ? wrapper as ModulePipelineWrapper<TRequest, TResponse>
            : default;
    }

    public void SetOptionAction(Func<IServiceProvider, IModuleProvider> moduleProviderFunc)
    {
        ModuleProviderFunc = moduleProviderFunc;
    }

    public virtual Type GetAttributeType()
    {
        return default;
    }

    public virtual IModuleProvider GetModuleProvider()
    {
        return default;
    }

    public abstract int PipelineOrder { get; }

    public virtual IModuleProvider GetDefaultProvider() => default;

    public virtual IModuleConfig GetModuleConfig(HandleIdentifier moduleHandleIdentifier)
    {
        var moduleConfig = ServiceProvider.GetKeyedService<ModuleConfig>(moduleHandleIdentifier);
        return moduleConfig;
    }

    public virtual string GetModuleName() => GetAttributeType().Name;

    public virtual HandleIdentifier GetModuleKey<TRequest, TResponse>()
    {
        var identifier = HandleIdentifier.From<TRequest, TResponse>(GetModuleName());
        return identifier;
    }

    public abstract ModulePipelineWrapper<TRequest, TResponse> GetModule<TRequest, TResponse>()
        where TRequest : notnull
        where TResponse : notnull;
}