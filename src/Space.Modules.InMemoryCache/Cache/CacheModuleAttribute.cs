using Space.Abstraction.Modules.Contracts;
using System;

namespace Space.Modules.InMemoryCache.Cache;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class CacheModuleAttribute : Attribute, ISpaceModuleAttribute
{
    public int Duration { get; set; }
}
