using Microsoft.Extensions.DependencyInjection;
using System;

namespace Space.Abstraction.Modules.Audit;

public static class AuditModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddSpaceAudit(this IServiceCollection services, Action<AuditModuleOptions> optionAction)
    {
        AuditModuleOptions opt = new();
        optionAction?.Invoke(opt);

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

                                  return moduleProvider is not IAuditModuleProvider auditModuleProvider
                                      ? throw new InvalidOperationException("No AuditModuleProvider is injected")
                                      : auditModuleProvider;
                              });

        return services;
    }
}
