namespace Space.Abstraction.Modules.Retry;

public class RetryModuleConfig : IModuleConfig, IRetrySettingsProperties
{
    public int RetryCount { get; set; }
    public int DelayMilliseconds { get; set; }
}
