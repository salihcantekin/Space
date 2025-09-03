# Known Issues

- Circular dependency in `ISpace` and `SpaceRegistry`: The first handler receives a null `ISpace(Lazy)` instance, which is set on subsequent requests.
- When using handler names, modules are applied to all handlers instead of only those with the attribute.

Refer to [ProjectDoc.txt](ProjectDoc.txt) for more details.
