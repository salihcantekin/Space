using System.Collections.Generic;

namespace Space.Abstraction.Modules;

/// <summary>
/// Provides access to global profile configurations for a specific module.
/// Example: for Audit -> IReadOnlyDictionary&lt;string, AuditModuleOptions&gt;
/// </summary>
public interface IModuleGlobalOptionsAccessor<TModuleOptions>
    where TModuleOptions : class
{
    IReadOnlyDictionary<string, TModuleOptions> Profiles { get; }
}

public sealed class ModuleGlobalOptionsAccessor<TModuleOptions>(IReadOnlyDictionary<string, TModuleOptions> profiles) : IModuleGlobalOptionsAccessor<TModuleOptions>
    where TModuleOptions : class
{
    public IReadOnlyDictionary<string, TModuleOptions> Profiles { get; } = profiles ?? new Dictionary<string, TModuleOptions>();
}
