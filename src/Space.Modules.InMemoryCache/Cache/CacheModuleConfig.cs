using Space.Abstraction.Modules.Contracts;
using System;

namespace Space.Modules.InMemoryCache.Cache;

public class CacheModuleConfig : IModuleConfig
{
    public TimeSpan TimeSpan { get; set; }
}

