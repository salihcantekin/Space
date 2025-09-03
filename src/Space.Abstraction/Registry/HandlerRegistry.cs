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
        Func<HandlerContext<TRequest>, ValueTask<TResponse>> handler,
        string name = "",
        IEnumerable<Func<PipelineContext<TRequest>, PipelineDelegate<TRequest, TResponse>, ValueTask<TResponse>>> pipelines = null)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more handlers can be registered.");

        var key = SpaceRegistry.GenerateKey<TRequest>(name);
        var entry = new SpaceRegistry.HandlerEntry<TRequest, TResponse>(handler, pipelines);

        handlerMap[key] = entry;
        handlerMapByType[typeof(TRequest)] = entry;
    }

    public void RegisterPipeline<TRequest, TResponse>(string handlerName, PipelineConfig pipelineConfig,
        Func<PipelineContext<TRequest>, PipelineDelegate<TRequest, TResponse>, ValueTask<TResponse>> pipeline)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more pipelines can be registered.");

        var key = SpaceRegistry.GenerateKey<TRequest>(handlerName);

        if (handlerMap.TryGetValue(key, out var handlerObj) && handlerObj is SpaceRegistry.HandlerEntry<TRequest, TResponse> entry)
        {
            entry.AddPipeline(pipeline, pipelineConfig);
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

    public ValueTask<TResponse> DispatchHandler<TRequest, TResponse>(HandlerContext<TRequest> ctx, string name = "")
    {
        if (!isSealed)
            throw new InvalidOperationException("Registration is not sealed. Call CompleteRegistration() before dispatching.");

        var key = SpaceRegistry.GenerateKey<TRequest>(name);

        space ??= serviceProvider.GetService<ISpace>();
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
        if (!isSealed)
            throw new InvalidOperationException("Registration is not sealed. Call CompleteRegistration() before dispatching.");

        var type = request.GetType();

        space ??= serviceProvider.GetService<ISpace>();
        Space ??= space;

        if (string.IsNullOrEmpty(name))
        {
            if (readOnlyHandlerMapByType.TryGetValue(type, out var handlerObj) && handlerObj is SpaceRegistry.IObjectHandlerEntry objectHandler)
            {
                var ctx = HandlerContextStruct.Create(serviceProvider, request, space, ct);
                return objectHandler.InvokeObject(ctx);
            }

            throw new InvalidOperationException($"Handler not found for type {type}");
        }

        if (readOnlyHandlerMap.TryGetValue((type, name), out var handlerObj2) && handlerObj2 is SpaceRegistry.IObjectHandlerEntry objectHandler2)
        {
            var ctx = HandlerContextStruct.Create(serviceProvider, request, space, ct);
            return objectHandler2.InvokeObject(ctx);
        }

        throw new InvalidOperationException($"Handler not found for type {type} and name '{name}'");
    }
}
