using Microsoft.Extensions.Options;
using Space.Abstraction;
using Space.Abstraction.Modules;
using System.Collections.Generic;

namespace Space.DependencyInjection;

public class ModuleProfileProvider : IModuleProfileProvider
{
    private readonly SpaceOptions _spaceOptions;

    public ModuleProfileProvider(IOptions<SpaceOptions> spaceOptions)
    {
        _spaceOptions = spaceOptions?.Value ?? new SpaceOptions();
    }

    public Dictionary<string, object> GetModuleProfileConfiguration(string moduleName, string profileName)
    {
        return _spaceOptions.GetModuleProfileConfiguration(moduleName, profileName);
    }
}