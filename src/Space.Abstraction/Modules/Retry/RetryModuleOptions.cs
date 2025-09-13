using System;

namespace Space.Abstraction.Modules.Retry;

public class RetryModuleOptions : ProfileModuleOptions<RetryModuleOptions>, IRetrySettingsProperties
{
    public int RetryCount { get; set; }
    public int DelayMilliseconds { get; set; }

    public RetryModuleOptions WithRetryModule<TProvider>(TProvider provider) where TProvider : IModuleProvider
    {
        WithModuleProvider(provider);
        return this;
    }

    public RetryModuleOptions WithRetryModule(Func<IServiceProvider, IModuleProvider> providerFunc)
    {
        WithModuleProvider(providerFunc);
        return this;
    }
}
