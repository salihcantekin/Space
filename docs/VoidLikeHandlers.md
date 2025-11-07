# Void-like Handlers (Non-generic Task / ValueTask)

Space allows handler methods marked with `[Handle]` to return either `Task<T>` / `ValueTask<T>` (the normal pathway) or the non-generic `Task` / `ValueTask` forms. When a non-generic async return is used, the framework automatically normalizes it to the logical `Nothing` response type. Internally the pipeline treats it exactly like `ValueTask<Nothing>` for consistency and composition.

## When To Use
Use a non-generic `Task` / `ValueTask` when:
- The operation produces no meaningful result (side?effect only) but should still participate in pipelines / modules.
- You want slightly cleaner handler signatures without inventing a dummy response type.
- You want to invoke the operation through the non-generic `Send(object request, ...)` convenience overload.

Avoid it when:
- The operation really returns data (then use a typed `ValueTask<TResponse>` for clarity and maximum fast?path optimizations).

## Rules & Validation
- The single parameter must be `HandlerContext<TRequest>` as with any standard handler.
- If `TRequest` implements `IRequest<T>` and `T` is not `Nothing`, returning non-generic `Task` / `ValueTask` is invalid.
  - The source generator emits diagnostic `HANDLE014` in this case.
- If `TRequest` implements `IRequest<Nothing>`, non-generic forms are allowed.
- If `TRequest` does not implement any `IRequest<T>` interface, non-generic forms are allowed (they implicitly map to `Nothing`).

### Diagnostic: HANDLE014
Message: `Non-generic Task/ValueTask return can only be used when request does not implement IRequest<T> or implements IRequest<Nothing>.`

Fix options:
1. Change return type to `ValueTask<TResponse>` (or `Task<TResponse>`).
2. Change the request to implement `IRequest<Nothing>` instead of `IRequest<SomeOtherType>`.
3. Remove the `IRequest<T>` implementation if no response is needed.

## Examples
```csharp
public record CreateUser(string Name) : IRequest<Nothing>;

public class UserHandlers
{
    [Handle]
    public ValueTask Create(HandlerContext<CreateUser> ctx)
    {
        // perform work (persist, publish events, etc.)
        return ValueTask.CompletedTask; // becomes ValueTask<Nothing>
    }

    [Handle(Name = "Alt")]
    public Task CreateAlt(HandlerContext<CreateUser> ctx)
        => Task.CompletedTask; // becomes Task<Nothing>
}

// Dispatch options
await space.Send<CreateUser, Nothing>(new CreateUser("demo"));      // strongly typed
await space.Send(new CreateUser("demo"), name: "Alt");             // non-generic convenience (maps to Nothing)
```

## Interaction With Pipelines & Modules
- Pipelines see a consistent logical `Nothing` response type – no special casing required.
- System / module pipelines (e.g. caching, retry, audit) participate normally. (Caching generally skips `Nothing` responses.)
- Fast-path optimizations still apply; if the non-generic operation completes synchronously the framework avoids extra awaits.

## Object / IRequest Overloads
- `ISpace.Send(object request, ...)` dispatches using runtime type inference and treats non-generic handlers as producing `Nothing`.
- `ISpace.Send<TResponse>(IRequest<TResponse> request, ...)` will never bind to a non-generic handler unless `TResponse` is `Nothing`.

## Performance Notes
- The wrapper allocation is avoided; the `Nothing` struct singleton value is reused.
- Choose `ValueTask` for minimal allocations in high-throughput scenarios.

## Summary
Non-generic async handlers are a concise way to express fire?and?complete operations while still benefiting from:
- Ordered pipelines
- Module interception
- Fast-path dispatch
- Unified `Nothing` response semantics

If you later need to return data, just switch to `ValueTask<TResponse>`; no other changes are required.
