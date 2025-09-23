using Microsoft.Extensions.Options;

namespace Space.Abstraction.Modules.Options;

/// <summary>
/// Provides module-specific options with support for attribute overrides and profile-based configuration.
/// </summary>
/// <typeparam name="TOptions">The strongly-typed options class</typeparam>
public interface IModuleOptionsProvider<TOptions> where TOptions : class, new()
{
    /// <summary>
    /// Gets the resolved options for a specific module instance, applying attribute overrides and profile settings.
    /// </summary>
    /// <param name="moduleIdentifier">The module identifier containing profile and attribute information</param>
    /// <returns>The resolved options instance</returns>
    TOptions GetOptions(ModuleIdentifier moduleIdentifier);
    
    /// <summary>
    /// Gets the resolved options for a specific profile name.
    /// </summary>
    /// <param name="profileName">The profile name (defaults to "Default" if null)</param>
    /// <returns>The resolved options instance</returns>
    TOptions GetOptions(string profileName = null);
}
