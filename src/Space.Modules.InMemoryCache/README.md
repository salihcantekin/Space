# Space.Modules.InMemoryCache

In-memory cache module for the Space framework. Adds a caching layer in the handler pipeline when a method is annotated.

## Install
```bash
 dotnet add package Space.Modules.InMemoryCache
```

## Registration
If the package is referenced, `AddSpace()` will detect the module. To add the default in-memory provider:
```csharp
services.AddSpace();
services.AddSpaceInMemoryCache(); // registers InMemoryCacheModuleProvider
```
Or just:
```csharp
services.AddSpaceInMemoryCache().AddSpace(); // order does not matter for detection
```

## Usage
Apply `[CacheModule]` on a handler method (together with `[Handle]`). The optional `Duration` (seconds) influences the internal config `TimeSpan`.
```csharp
public class UserQueries
{
    [Handle]
    [CacheModule(Duration = 60)] // seconds
    public ValueTask<List<UserDetail>> GetTopUsers(HandlerContext<int> ctx)
    {
        // access services via ctx.ServiceProvider
        // return cached value if present (module manages this)
    }
}
```

## How It Works
- Source generator detects `[CacheModule]` usage and prepares module metadata.
- At runtime the module resolves an `ICacheModuleProvider` (default registered by `AddSpaceInMemoryCache`).
- The provider supplies keys and stores / retrieves values.
- Module order (`PipelineOrder = int.MinValue + 2`) ensures it runs before user pipelines but after any earlier system modules (e.g. Audit if introduced with lower order).

## Custom Provider
Implement `ICacheModuleProvider` to change key strategy or backing store (e.g. Redis):
```csharp
public sealed class RedisCacheModuleProvider : ICacheModuleProvider
{
    private readonly ConcurrentDictionary<string, object> store = new();

    public string GetKey<TRequest>(TRequest request) => request?.ToString();

    public ValueTask Store<TResponse>(string key, TResponse response, CacheModuleConfig cfg)
    { store[key] = response; return default; }

    public bool TryGet<TResponse>(string key, out TResponse response, CacheModuleConfig cfg)
    {
        response = default;
        if (!store.TryGetValue(key, out var obj)) return false;
        response = (TResponse)obj; return true;
    }
}
```
Register it instead of the default:
```csharp
services.AddSingleton<ICacheModuleProvider, RedisCacheModuleProvider>();
services.AddSpace();
```

## Configuration Mapping
`Duration` (int seconds) from the attribute populates `CacheModuleConfig.TimeSpan`. If omitted or < 0 it is treated as 0.

## Notes
- A module attribute augments its `[Handle]` method; it does not introduce its own method.
- Only handlers explicitly annotated with `[CacheModule]` should be cached (framework ensures attribute scoping per handler signature).

## Links
- Repo: https://github.com/salihcantekin/Space
- License: MIT
