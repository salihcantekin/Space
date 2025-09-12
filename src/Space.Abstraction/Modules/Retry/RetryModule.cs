using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules.Retry;

[SpaceModule(ModuleAttributeType = typeof(RetryModuleAttribute))]
public class RetryModule(IServiceProvider serviceProvider) : SpaceModule(serviceProvider)
{
    private IModuleProvider moduleProvider;

    public override int PipelineOrder => int.MinValue + 2; // run after Audit for demo

    public override Type GetAttributeType() => typeof(RetryModuleAttribute);

    private IReadOnlyDictionary<string, RetryModuleOptions> GetGlobalProfiles()
    {
        return ServiceProvider.GetService<IModuleGlobalOptionsAccessor<RetryModuleOptions>>()?.Profiles
               ?? new Dictionary<string, RetryModuleOptions>();
    }

    private static IReadOnlyDictionary<string, object> ExtractProfileProperties(RetryModuleOptions profileOpt)
    {
        return profileOpt is null
                ? []
                : RetrySettingsPropertiesMapper.ToDictionary(profileOpt);
    }

    private static string NormalizeProfileName(string name)
        => string.IsNullOrWhiteSpace(name) ? ModuleConstants.DefaultProfileName : name;

    public override IModuleConfig GetModuleConfig(ModuleIdentifier moduleKey)
    {
        return GetOrAddConfig(moduleKey, () =>
        {
            IReadOnlyDictionary<string, object> defaultProps = new Dictionary<string, object>();

            var globalProfiles = GetGlobalProfiles();
            var requested = NormalizeProfileName(moduleKey.ProfileName);
            if (!globalProfiles.TryGetValue(requested, out var profileOpt))
            {
                globalProfiles.TryGetValue(ModuleConstants.DefaultProfileName, out profileOpt);
            }
            var globalProfileProps = ExtractProfileProperties(profileOpt);

            var attributeConfig = ServiceProvider.GetKeyedService<ModuleConfig>(moduleKey);
            var attributeProps = attributeConfig?.GetAllModuleProperties();

            var merged = ModuleConfigMerge.Merge(defaultProps, globalProfileProps, attributeProps);

            var cfg = new RetryModuleConfig();
            RetrySettingsPropertiesMapper.ApplyTo(cfg, merged);

            return cfg;
        });
    }

    public override IModuleProvider GetDefaultProvider() => new DefaultRetryModuleProvider();

    public override IModuleProvider GetModuleProvider()
    {
        if (moduleProvider is not null)
            return moduleProvider;

        moduleProvider = ServiceProvider.GetService<IRetryModuleProvider>() ?? GetDefaultProvider();
        return moduleProvider;
    }

    public override ModulePipelineWrapper<TRequest, TResponse> GetModule<TRequest, TResponse>(string profileName)
    {
        var identifier = base.GetModuleKey<TRequest, TResponse>(profileName);
        var cachedWrapper = GetWrapper<TRequest, TResponse>(identifier);
        if (cachedWrapper is not null)
            return cachedWrapper;

        var provider = (IRetryModuleProvider)GetModuleProvider();
        var config = (RetryModuleConfig)GetModuleConfig(identifier);

        cachedWrapper = new RetryModulePipelineWrapper<TRequest, TResponse>(config, provider);
        CacheWrapper(identifier, cachedWrapper);

        return cachedWrapper;
    }
}
