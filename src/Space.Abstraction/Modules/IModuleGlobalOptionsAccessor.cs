using System.Collections.Generic;

namespace Space.Abstraction.Modules;

/// <summary>
/// Provides access to global profile configurations for a specific module.
/// Example: for Audit -> IReadOnlyDictionary&lt;string, AuditModuleOptions&gt;
/// </summary>
public interface IModuleGlobalOptionsAccessor<TModuleOptions>
    where TModuleOptions : BaseModuleOptions
{
    IReadOnlyDictionary<string, TModuleOptions> Profiles { get; }
}

public sealed class ModuleGlobalOptionsAccessor<TModuleOptions> : IModuleGlobalOptionsAccessor<TModuleOptions>
    where TModuleOptions : BaseModuleOptions
{
    public ModuleGlobalOptionsAccessor(IReadOnlyDictionary<string, TModuleOptions> profiles)
    {
        Profiles = profiles ?? new Dictionary<string, TModuleOptions>();
    }

    public IReadOnlyDictionary<string, TModuleOptions> Profiles { get; }
}
