using System.Collections.Generic;

namespace Space.Abstraction.Modules;

public interface IModuleProfileProvider
{
    Dictionary<string, object> GetModuleProfileConfiguration(string moduleName, string profileName);
}