using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Space.Modules.InMemoryCache.Cache;

public sealed class InMemoryCacheModuleProvider : ICacheModuleProvider
{
    private readonly ConcurrentDictionary<string, object> handlers = [];

    public string GetKey<TRequest>(TRequest request)
    {
        return request?.ToString();
    }

    public ValueTask Store<TResponse>(string key, TResponse response, CacheModuleConfig config)
    {
        handlers[key] = response;
        return default;
    }

    public bool TryGet<TResponse>(string key, out TResponse response, CacheModuleConfig config)
    {
        response = default;

        if (!handlers.TryGetValue(key, out var objResponse))
            return false;

        response = (TResponse)objResponse;
        return true;

    }
}