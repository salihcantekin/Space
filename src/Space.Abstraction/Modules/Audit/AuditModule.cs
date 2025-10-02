using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules.Audit;


[SpaceModule(ModuleAttributeType = typeof(AuditModuleAttribute))]
public class AuditModule(IServiceProvider serviceProvider) : SpaceModule(serviceProvider)
{
    private IModuleProvider moduleProvider;

    public override int PipelineOrder => int.MinValue + 1;

    public override Type GetAttributeType() => typeof(AuditModuleAttribute);

    private IReadOnlyDictionary<string, AuditModuleOptions> GetGlobalProfiles()
    {
        return ServiceProvider.GetService<IModuleGlobalOptionsAccessor<AuditModuleOptions>>()?.Profiles
               ?? new Dictionary<string, AuditModuleOptions>();
    }

    private static IReadOnlyDictionary<string, object> ExtractProfileProperties(AuditModuleOptions profileOpt)
    {
        if (profileOpt is null)
            return new Dictionary<string, object>();

        return AuditSettingsPropertiesMapper.ToDictionary(profileOpt);
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

            var cfg = new AuditModuleConfig();
            AuditSettingsPropertiesMapper.ApplyTo(cfg, merged);
            return cfg;
        });
    }

    public override IModuleProvider GetDefaultProvider() => new NullAuditModuleProvider();

    public override IModuleProvider GetModuleProvider()
    {
        if (moduleProvider is not null)
            return moduleProvider;

        moduleProvider = ServiceProvider.GetService<IAuditModuleProvider>() ?? GetDefaultProvider();

        return moduleProvider;
    }

    private static string TrimGlobal(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return typeName;
        const string prefix = "global::";

        return typeName.StartsWith(prefix, StringComparison.Ordinal) ? typeName.Substring(prefix.Length) : typeName;
    }

    private static Type ResolveType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        var tn = TrimGlobal(typeName);
        var t = Type.GetType(tn, throwOnError: false);
        return t ?? null;
    }

    private IAuditModuleProvider TryGetAttributeProvider(ModuleIdentifier id)
    {
        var attributeConfig = ServiceProvider.GetKeyedService<ModuleConfig>(id);
        var providerName = attributeConfig?.GetModuleProperty("Provider") as string;
        if (string.IsNullOrEmpty(providerName))
            return null;

        var t = ResolveType(providerName);
        if (t == null)
            return null;

        if (ServiceProvider.GetService(t) is IAuditModuleProvider svc)
            return svc;

        try
        {
            var obj = ActivatorUtilities.CreateInstance(ServiceProvider, t);
            return obj as IAuditModuleProvider;
        }
        catch
        {
            return null;
        }
    }

    public override ModulePipelineWrapper<TRequest, TResponse> GetModule<TRequest, TResponse>(string profileName)
    {
        var auditIdentifier = base.GetModuleKey<TRequest, TResponse>(profileName);
        var cachedWrapper = GetWrapper<TRequest, TResponse>(auditIdentifier);

        if (cachedWrapper is not null)
            return cachedWrapper;

        var provider = TryGetAttributeProvider(auditIdentifier) ?? (IAuditModuleProvider)GetModuleProvider();

        if (provider is not null)
            AuditModulePipelineWrapper<TRequest, TResponse>.UseCustomAuditProvider(provider);

        var auditConfig = (AuditModuleConfig)GetModuleConfig(auditIdentifier);
        var wrapper = new AuditModulePipelineWrapper<TRequest, TResponse>();

        CacheWrapper(auditIdentifier, wrapper);

        return wrapper;
    }
}
