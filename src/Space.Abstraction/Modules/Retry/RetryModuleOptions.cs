using System;

namespace Space.Abstraction.Modules.Retry;

/// <summary>
/// Legacy retry module options class for backward compatibility.
/// 
/// ⚠️ DEPRECATED: This class is deprecated. Use RetryOptions with the new Options pattern instead.
/// 
/// Migration example:
/// OLD: services.AddSpaceRetry(opt => opt.WithDefaultProfile(p => p.RetryCount = 5));
/// NEW: services.AddSpaceRetryOptions(opt => opt.RetryCount = 5);
/// </summary>
[Obsolete("Use RetryOptions with AddSpaceRetryOptions() instead. This class will be removed in a future version.")]
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
