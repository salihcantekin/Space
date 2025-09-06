using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction.Registry;

public sealed class HandlerRegistry(IServiceProvider serviceProvider)
{
    private Dictionary<(Type, string, Type), object> handlerMap = [];
    private Dictionary<(Type, Type), object> handlerMapByType = [];
    private ReadOnlyDictionary<(Type, string, Type), object> readOnlyHandlerMap;
    private ReadOnlyDictionary<(Type, Type), object> readOnlyHandlerMapByType;
    private bool isSealed = false;
    private ISpace space;

    public static ISpace Space { get; private set; }

    public void RegisterHandler<TRequest, TResponse>(
        HandlerInvoker<TRequest, TResponse> invoker,
        string name = "",
        IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelines = null,
        LightHandlerInvoker<TRequest, TResponse> lightInvoker = null)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more handlers can be registered.");

        var key = SpaceRegistry.GenerateKey<TRequest, TResponse>(name);
        var entry = new SpaceRegistry.HandlerEntry<TRequest, TResponse>(invoker, lightInvoker, pipelines);

        handlerMap[key] = entry;
        handlerMapByType[(typeof(TRequest), typeof(TResponse))] = entry;
    }

    public void RegisterPipeline<TRequest, TResponse>(string handlerName, PipelineConfig pipelineConfig,
        PipelineInvoker<TRequest, TResponse> pipelineInvoker)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more pipelines can be registered.");

        var key = SpaceRegistry.GenerateKey<TRequest, TResponse>(handlerName);

        if (handlerMap.TryGetValue(key, out var handlerObj) && handlerObj is SpaceRegistry.HandlerEntry<TRequest, TResponse> entry)
        {
            entry.AddPipeline(pipelineInvoker, pipelineConfig);
        }
    }

    public void CompleteRegistration()
    {
        if (!isSealed)
        {
            readOnlyHandlerMap = new ReadOnlyDictionary<(Type, string, Type), object>(handlerMap);
            readOnlyHandlerMapByType = new ReadOnlyDictionary<(Type, Type), object>(handlerMapByType);
            isSealed = true;

            handlerMap = null;
            handlerMapByType = null;
        }
    }

    internal bool TryGetHandlerEntry<TRequest, TResponse>(string name, out SpaceRegistry.HandlerEntry<TRequest, TResponse> entry)
    {
        entry = null;
        if (!isSealed) return false;
        var key = SpaceRegistry.GenerateKey<TRequest, TResponse>(name);
        if (readOnlyHandlerMap != null && readOnlyHandlerMap.TryGetValue(key, out var obj) && obj is SpaceRegistry.HandlerEntry<TRequest, TResponse> he)
        { entry = he; return true; }
        if (string.IsNullOrEmpty(name) && readOnlyHandlerMapByType != null && readOnlyHandlerMapByType.TryGetValue((typeof(TRequest), typeof(TResponse)), out var byType) && byType is SpaceRegistry.HandlerEntry<TRequest, TResponse> he2)
        { entry = he2; return true; }
        return false;
    }

    internal bool TryGetHandlerEntryByRuntimeType(Type requestType, Type responseType, string name, out object entryObj)
    {
        entryObj = null;
        if (!isSealed) return false;
        if (string.IsNullOrEmpty(name))
        {
            if (readOnlyHandlerMapByType != null && readOnlyHandlerMapByType.TryGetValue((requestType, responseType), out var direct))
            { entryObj = direct; return true; }
        }
        else if (readOnlyHandlerMap != null && readOnlyHandlerMap.TryGetValue((requestType, name, responseType), out var named))
        { entryObj = named; return true; }
        return false;
    }

    public ValueTask<TResponse> DispatchHandler<TRequest, TResponse>(IServiceProvider execProvider, HandlerContext<TRequest> ctx, string name = "")
    {
        if (!isSealed)
            throw new InvalidOperationException("Registration is not sealed. Call CompleteRegistration() before dispatching.");

        var key = SpaceRegistry.GenerateKey<TRequest, TResponse>(name);

        space ??= execProvider.GetService<ISpace>();
        Space ??= space;

        if (readOnlyHandlerMap.TryGetValue(key, out var handlerObj) && handlerObj is SpaceRegistry.HandlerEntry<TRequest, TResponse> entry)
        {
            return entry.Invoke(ctx);
        }

        if (readOnlyHandlerMapByType.TryGetValue((typeof(TRequest), typeof(TResponse)), out var typeHandlerObj) && typeHandlerObj is SpaceRegistry.HandlerEntry<TRequest, TResponse> typeEntry)
        {
            return typeEntry.Invoke(ctx);
        }

        throw new InvalidOperationException($"Handler not found for type {typeof(TRequest)} -> {typeof(TResponse)} and name '{name}'");
    }

    public ValueTask<object> DispatchHandler(object request, string name = "", CancellationToken ct = default)
        => DispatchHandlerInternal(request, name, null, serviceProvider, ct);

    public ValueTask<object> DispatchHandler(object request, string name, IServiceProvider executionProvider, CancellationToken ct = default)
        => DispatchHandlerInternal(request, name, null, executionProvider ?? serviceProvider, ct);

    public ValueTask<object> DispatchHandler(object request, string name, Type responseType, IServiceProvider executionProvider, CancellationToken ct = default)
        => DispatchHandlerInternal(request, name, responseType, executionProvider ?? serviceProvider, ct);

    public ValueTask<TResponse> DispatchHandler<TRequest, TResponse>(object request, string name, IServiceProvider executionProvider, CancellationToken ct = default)
    {
        if (request is not TRequest typed)
            throw new InvalidOperationException($"Request type mismatch. Expected {typeof(TRequest)}, got {request?.GetType()}");
        var ctx = HandlerContext<TRequest>.Create(executionProvider, typed, ct);
        return DispatchHandler<TRequest, TResponse>(executionProvider, ctx, name);
    }

    private ValueTask<object> DispatchHandlerInternal(object request, string name, Type responseType, IServiceProvider execProvider, CancellationToken ct)
    {
        if (!isSealed)
            throw new InvalidOperationException("Registration is not sealed. Call CompleteRegistration() before dispatching.");

        var type = request.GetType();

        space ??= execProvider.GetService<ISpace>();
        Space ??= space;

        if (string.IsNullOrEmpty(name))
        {
            // Prefer disambiguated lookup when responseType provided
            if (responseType != null)
            {
                if (readOnlyHandlerMapByType.TryGetValue((type, responseType), out var handlerObjRt) && handlerObjRt is SpaceRegistry.IObjectHandlerEntry objectHandlerRt)
                {
                    var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
                    return objectHandlerRt.InvokeObject(ctx);
                }
                throw new InvalidOperationException($"Handler not found for type {type} -> {responseType}");
            }
            else if (readOnlyHandlerMapByType.TryGetValue((type, typeof(object)), out var objHandler) && objHandler is SpaceRegistry.IObjectHandlerEntry objectHandler)
            {
                var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
                return objectHandler.InvokeObject(ctx);
            }
            else
            {
                // Fallback: try to find a single entry for the request type when response type is unknown
                if (readOnlyHandlerMapByType != null)
                {
                    // enumerate to detect single candidate
                    int found = 0; SpaceRegistry.IObjectHandlerEntry last = null;
                    foreach (var kv in readOnlyHandlerMapByType)
                    {
                        if (kv.Key.Item1 == type && kv.Value is SpaceRegistry.IObjectHandlerEntry entry)
                        { found++; last = entry; if (found > 1) break; }
                    }
                    if (found == 1)
                    {
                        var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
                        return last.InvokeObject(ctx);
                    }
                }
                throw new InvalidOperationException($"Handler not found for type {type}");
            }
        }

        if (responseType != null)
        {
            if (readOnlyHandlerMap.TryGetValue((type, name, responseType), out var handlerObj2) && handlerObj2 is SpaceRegistry.IObjectHandlerEntry objectHandler2)
            {
                var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
                return objectHandler2.InvokeObject(ctx);
            }
            throw new InvalidOperationException($"Handler not found for type {type} -> {responseType} and name '{name}'");
        }
        else
        {
            // legacy path (may be ambiguous if multiple responses exist)
            foreach (var kv in readOnlyHandlerMap)
            {
                if (kv.Key.Item1 == type && kv.Key.Item2 == (name ?? string.Empty) && kv.Value is SpaceRegistry.IObjectHandlerEntry oh)
                {
                    var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
                    return oh.InvokeObject(ctx);
                }
            }
        }

        throw new InvalidOperationException($"Handler not found for type {type} and name '{name}'");
    }
}
