using System;

namespace Space.Abstraction.Modules.Retry;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class RetryModuleAttribute : Attribute, ISpaceModuleAttribute
{
    public string Profile { get; set; } = "Default";
    public int RetryCount { get; set; } = 3;
    public int DelayMilliseconds { get; set; } = 0;

    public RetryModuleAttribute() { }
    public RetryModuleAttribute(string profileName)
    {
        Profile = profileName;
    }
    public RetryModuleAttribute(string profileName, int retryCount)
    {
        Profile = profileName;
        RetryCount = retryCount;
    }
}
