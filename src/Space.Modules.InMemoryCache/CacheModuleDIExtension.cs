using Microsoft.Extensions.DependencyInjection;
using Space.Modules.InMemoryCache.Cache;

namespace Space.Modules.InMemoryCache;

public static class CacheModuleDependencyInjectionExtensions
{
    public static IServiceCollection AddSpaceInMemoryCache(this IServiceCollection services)
    {
        services.AddSingleton<ICacheModuleProvider, InMemoryCacheModuleProvider>();

        return services;
    }
}
