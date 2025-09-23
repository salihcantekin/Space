using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Space.Abstraction.Modules.Options;
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
            // Try new Options pattern first, fallback to legacy approach
            var optionsProvider = ServiceProvider.GetService<IModuleOptionsProvider<RetryOptions>>();
            if (optionsProvider != null)
            {
                var options = optionsProvider.GetOptions(moduleKey);
                // Convert RetryOptions to RetryModuleConfig for backward compatibility
                return new RetryModuleConfig
                {
                    RetryCount = options.RetryCount,
                    DelayMilliseconds = options.DelayMilliseconds
                };
            }

            // Legacy approach for backward compatibility
            return ServiceProvider.GetLegacyRetryConfig(moduleKey);
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
