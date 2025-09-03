# Planned Improvements

- Module attributes should accept custom module parameters, e.g., `[Cache(Provider = typeof(RedisCacheProvider))]`
- Module parameters should support application-wide default values, e.g., `services.AddCache(opt => opt.Duration = TimeSpan.FromHours(1));`
- Module configs should use the Options pattern for easier access within providers.

See [ProjectDoc.txt](ProjectDoc.txt) for the full roadmap.
