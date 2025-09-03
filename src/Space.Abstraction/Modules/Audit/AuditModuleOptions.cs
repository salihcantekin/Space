using System;

namespace Space.Abstraction.Modules.Audit;

public class AuditModuleOptions : BaseModuleOptions
{
    public AuditModuleOptions WithAuditModule<TProvider>(TProvider provider) where TProvider : IModuleProvider
    {
        WithModuleProvider(provider);

        return this;
    }

    public AuditModuleOptions WithAuditModule(Func<IServiceProvider, IModuleProvider> providerFunc)
    {
        WithModuleProvider(providerFunc);

        return this;
    }
}
