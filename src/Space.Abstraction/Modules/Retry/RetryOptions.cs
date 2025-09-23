using System;

namespace Space.Abstraction.Modules.Retry;

/// <summary>
/// Strongly-typed options for the Retry module following the .NET Options pattern.
/// </summary>
public class RetryOptions : IRetrySettingsProperties
{
    /// <summary>
    /// The number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// The delay between retry attempts in milliseconds.
    /// </summary>
    public int DelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Whether to use exponential backoff for retry delays.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = false;

    /// <summary>
    /// The maximum delay between retries when using exponential backoff (in milliseconds).
    /// </summary>
    public int MaxDelayMilliseconds { get; set; } = 30000;

    /// <summary>
    /// The backoff multiplier for exponential backoff.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;
}
