using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction.Registry;

public sealed class HandlerRegistry(IServiceProvider serviceProvider)
{
    private Dictionary<(Type, string), object> handlerMap = [];
    private Dictionary<Type, object> handlerMapByType = [];
    private ReadOnlyDictionary<(Type, string), object> readOnlyHandlerMap;
    private ReadOnlyDictionary<Type, object> readOnlyHandlerMapByType;
    private bool isSealed = false;
    private ISpace space;

    public static ISpace Space { get; private set; }

    public void RegisterHandler<TRequest, TResponse>(
        HandlerInvoker<TRequest, TResponse> invoker,
        string name = "",
        IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelines = null)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more handlers can be registered.");

        var key = SpaceRegistry.GenerateKey<TRequest>(name);
        var entry = new SpaceRegistry.HandlerEntry<TRequest, TResponse>(invoker, pipelines);

        handlerMap[key] = entry;
        handlerMapByType[typeof(TRequest)] = entry;
    }

    public void RegisterPipeline<TRequest, TResponse>(string handlerName, PipelineConfig pipelineConfig,
        PipelineInvoker<TRequest, TResponse> pipelineInvoker)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more pipelines can be registered.");

        var key = SpaceRegistry.GenerateKey<TRequest>(handlerName);

        if (handlerMap.TryGetValue(key, out var handlerObj) && handlerObj is SpaceRegistry.HandlerEntry<TRequest, TResponse> entry)
        {
            entry.AddPipeline(pipelineInvoker, pipelineConfig);
        }
    }

    public void CompleteRegistration()
    {
        if (!isSealed)
        {
            readOnlyHandlerMap = new ReadOnlyDictionary<(Type, string), object>(handlerMap);
            readOnlyHandlerMapByType = new ReadOnlyDictionary<Type, object>(handlerMapByType);
            isSealed = true;

            handlerMap = null;
            handlerMapByType = null;
        }
    }

    public bool TryGetHandlerEntry<TRequest, TResponse>(string name, out SpaceRegistry.HandlerEntry<TRequest, TResponse> entry)
    {
        entry = null;
        if (!isSealed) return false;
        var key = SpaceRegistry.GenerateKey<TRequest>(name);
        if (readOnlyHandlerMap != null && readOnlyHandlerMap.TryGetValue(key, out var obj) && obj is SpaceRegistry.HandlerEntry<TRequest, TResponse> he)
        {
            entry = he; return true;
        }
        if (string.IsNullOrEmpty(name) && readOnlyHandlerMapByType != null && readOnlyHandlerMapByType.TryGetValue(typeof(TRequest), out var byType) && byType is SpaceRegistry.HandlerEntry<TRequest, TResponse> he2)
        { entry = he2; return true; }
        return false;
    }

    public ValueTask<TResponse> DispatchHandler<TRequest, TResponse>(IServiceProvider execProvider, HandlerContext<TRequest> ctx, string name = "")
    {
        if (!isSealed)
            throw new InvalidOperationException("Registration is not sealed. Call CompleteRegistration() before dispatching.");

        var key = SpaceRegistry.GenerateKey<TRequest>(name);

        space ??= execProvider.GetService<ISpace>();
        Space ??= space;

        if (readOnlyHandlerMap.TryGetValue(key, out var handlerObj) && handlerObj is SpaceRegistry.HandlerEntry<TRequest, TResponse> entry)
        {
            return entry.Invoke(ctx);
        }

        if (readOnlyHandlerMapByType.TryGetValue(typeof(TRequest), out var typeHandlerObj) && typeHandlerObj is SpaceRegistry.HandlerEntry<TRequest, TResponse> typeEntry)
        {
            return typeEntry.Invoke(ctx);
        }

        throw new InvalidOperationException($"Handler not found for type {typeof(TRequest)} and name '{name}'");
    }

    public ValueTask<object> DispatchHandler(object request, string name = "", CancellationToken ct = default)
    {
        return DispatchHandlerInternal(request, name, serviceProvider, ct);
    }

    public ValueTask<object> DispatchHandler(object request, string name, IServiceProvider executionProvider, CancellationToken ct = default)
    {
        return DispatchHandlerInternal(request, name, executionProvider ?? serviceProvider, ct);
    }

    public ValueTask<TResponse> DispatchHandler<TRequest, TResponse>(object request, string name, IServiceProvider executionProvider, CancellationToken ct = default)
    {
        if (request is not TRequest typed)
            throw new InvalidOperationException($"Request type mismatch. Expected {typeof(TRequest)}, got {request?.GetType()}");

        var ctx = HandlerContext<TRequest>.Create(executionProvider, typed, ct);
        return DispatchHandler<TRequest, TResponse>(executionProvider, ctx, name);
    }

    private ValueTask<object> DispatchHandlerInternal(object request, string name, IServiceProvider execProvider, CancellationToken ct)
    {
        if (!isSealed)
            throw new InvalidOperationException("Registration is not sealed. Call CompleteRegistration() before dispatching.");

        var type = request.GetType();

        space ??= execProvider.GetService<ISpace>();
        Space ??= space;

        if (string.IsNullOrEmpty(name))
        {
            if (readOnlyHandlerMapByType.TryGetValue(type, out var handlerObj) && handlerObj is SpaceRegistry.IObjectHandlerEntry objectHandler)
            {
                var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
                return objectHandler.InvokeObject(ctx);
            }

            throw new InvalidOperationException($"Handler not found for type {type}");
        }

        if (readOnlyHandlerMap.TryGetValue((type, name), out var handlerObj2) && handlerObj2 is SpaceRegistry.IObjectHandlerEntry objectHandler2)
        {
            var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
            return objectHandler2.InvokeObject(ctx);
        }

        throw new InvalidOperationException($"Handler not found for type {type} and name '{name}'");
    }
}
