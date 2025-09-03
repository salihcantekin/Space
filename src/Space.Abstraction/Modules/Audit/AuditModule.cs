using Microsoft.Extensions.DependencyInjection;
using System;

namespace Space.Abstraction.Modules.Audit;


[SpaceModule(ModuleAttributeType = typeof(AuditModuleAttribute), IsEnabled = true)]
public class AuditModule(IServiceProvider serviceProvider) : SpaceModule(serviceProvider)
{
    private IModuleProvider moduleProvider;
    private AuditModuleConfig auditModuleConfig;

    public override int PipelineOrder => int.MinValue + 1;

    public override Type GetAttributeType()
    {
        return typeof(AuditModuleAttribute);
    }

    public override IModuleConfig GetModuleConfig(HandleIdentifier moduleKey)
    {
        if (auditModuleConfig is not null)
            return auditModuleConfig;

        //var moduleConfig = ServiceProvider.GetKeyedService<ModuleConfig>(moduleKey);

        auditModuleConfig = new();

        return auditModuleConfig;
    }

    public override IModuleProvider GetModuleProvider()
    {
        if (moduleProvider is not null)
            return moduleProvider;

        moduleProvider = ServiceProvider.GetService<IAuditModuleProvider>() ?? GetDefaultProvider();

        return moduleProvider;
    }


    public override ModulePipelineWrapper<TRequest, TResponse> GetModule<TRequest, TResponse>()
    {
        var auditIdentifier = base.GetModuleKey<TRequest, TResponse>();
        var cachedWrapper = GetWrapper<TRequest, TResponse>(auditIdentifier);

        if (cachedWrapper is not null)
            return cachedWrapper;

        var auditModuleProvider = (IAuditModuleProvider)GetModuleProvider();
        var auditConfig = (AuditModuleConfig)GetModuleConfig(auditIdentifier);

        if (auditModuleProvider is not null)
            AuditModulePipelineWrapper<TRequest, TResponse>.UseCustomAuditProvider(auditModuleProvider);

        cachedWrapper = new AuditModulePipelineWrapper<TRequest, TResponse>(auditConfig);

        CacheWrapper(auditIdentifier, cachedWrapper);

        return cachedWrapper;
    }
}
