using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules.Retry;

public static class RetryModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddSpaceRetry(this IServiceCollection services, Action<RetryModuleOptions> optionAction)
    {
        var opt = new RetryModuleOptions();
        optionAction?.Invoke(opt);

        services.AddSingleton<IReadOnlyDictionary<string, RetryModuleOptions>>(opt.Profiles);
        services.AddSingleton<IModuleGlobalOptionsAccessor<RetryModuleOptions>>(sp => new ModuleGlobalOptionsAccessor<RetryModuleOptions>(opt.Profiles));

        services.AddSingleton(typeof(IRetryModuleProvider), sp =>
        {
            IModuleProvider moduleProvider = null;

            if (opt.ModuleProvider != null)
                moduleProvider = opt.ModuleProvider;
            else
            if (opt.ModuleProviderAction != null)
                moduleProvider = opt.ModuleProviderAction(sp);

            return moduleProvider as IRetryModuleProvider ?? new DefaultRetryModuleProvider();
        });

        return services;
    }
}
