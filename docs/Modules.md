# Modules

Modules in Space are system-level pipelines that perform predefined operations, such as caching or auditing. Modules are added via attributes to handler methods and require registration through extension methods.

## Built-in Modules
- **In-Memory Cache**: Add `[Cache(Duration = 60)]` to a handler and register with `services.AddSpaceInMemoryCache()`.
- **Audit**: Add `[Audit]` to a handler and register with `services.AddSpaceAudit()`.

## Custom Modules
You can implement your own modules by following the interface and configuration standards. See the `Space.Modules.InMemoryCache` for reference.

## Example: Redis Cache
```csharp
services.AddSpaceCache(opt =>
{
    opt.WithCacheModule(sp => new RedisModuleProvider());
});

public sealed class RedisModuleProvider : ICacheModuleProvider
{
    // Implementation details...
}
```

## Notes
- Modules must be registered via their extension methods.
- Custom modules should follow the required interfaces and provide a config model.
