namespace Space.Abstraction.Modules.Retry;

public interface IRetrySettingsProperties
{
    int RetryCount { get; set; }
    int DelayMilliseconds { get; set; }
}
