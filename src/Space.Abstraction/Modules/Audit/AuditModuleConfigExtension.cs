using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules.Audit;

public static class AuditModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddSpaceAudit(this IServiceCollection services, Action<AuditModuleOptions> optionAction)
    {
        AuditModuleOptions opt = new();
        optionAction?.Invoke(opt);

        services.AddSingleton<IReadOnlyDictionary<string, AuditModuleOptions>>(opt.Profiles);
        services.AddSingleton<IModuleGlobalOptionsAccessor<AuditModuleOptions>>(sp => new ModuleGlobalOptionsAccessor<AuditModuleOptions>(opt.Profiles));

        services.AddSingleton(typeof(IAuditModuleProvider),
                (sp) =>
                {
                    IModuleProvider moduleProvider = null;

                    if (opt.ModuleProvider != null)
                    {
                        moduleProvider = opt.ModuleProvider;
                    }
                    else if (opt.ModuleProviderAction != null)
                    {
                        moduleProvider = opt.ModuleProviderAction(sp);
                    }

                    if (moduleProvider is null)
                    {
                        // Fallback to no-op audit provider if not configured
                        return new NullAuditModuleProvider();
                    }

                    if (moduleProvider is not IAuditModuleProvider auditModuleProvider)
                        throw new InvalidOperationException("AuditModuleProvider must implement IAuditModuleProvider");

                    return auditModuleProvider;
                });

        return services;
    }
}
