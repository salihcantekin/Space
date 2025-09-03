using Space.Abstraction.Context;
using Space.Abstraction.Modules;
using System;
using System.Threading.Tasks;

namespace Space.Modules.InMemoryCache.Cache;

public class CacheModulePipelineWrapper<TRequest, TResponse>(CacheModuleConfig cacheConfig) : ModulePipelineWrapper<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    private static ICacheModuleProvider moduleProvider = new InMemoryCacheModuleProvider();

    // Optionally allow setting external provider (used for custom cache).
    public static void UseCustomCacheProvider(ICacheModuleProvider provider)
        => moduleProvider = provider ?? throw new ArgumentNullException(nameof(provider));

    public override async ValueTask<TResponse> HandlePipeline(PipelineContext<TRequest> ctx, PipelineDelegate<TRequest, TResponse> next)
    {
        var key = moduleProvider.GetKey(ctx.Request);
        if (moduleProvider.TryGet<TResponse>(key, out var cachedValue, cacheConfig))
        {
            //Console.WriteLine("The data returned from the REDIS CACHE");
            return cachedValue;
        }

        // call the actual handle method
        var response = await next(ctx);

        await moduleProvider.Store(key, response, cacheConfig);

        return response;
    }
}
