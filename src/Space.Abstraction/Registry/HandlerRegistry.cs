using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Space.Abstraction.Registry;

public sealed class HandlerRegistry(IServiceProvider serviceProvider)
{
    // Optimized dictionaries using struct keys for reduced allocation
    private Dictionary<HandlerKey, object> handlerMap = new(HandlerKeyComparer.Instance);
    private Dictionary<TypePairKey, object> handlerMapByType = new(TypePairKeyComparer.Instance);
    private Dictionary<TypePairKey, List<GlobalPipelineContainerInternal>> globalPipelineMap = new(TypePairKeyComparer.Instance);
    
    private ReadOnlyDictionary<HandlerKey, object> readOnlyHandlerMap;
    private ReadOnlyDictionary<TypePairKey, object> readOnlyHandlerMapByType;
    private ReadOnlyDictionary<TypePairKey, List<GlobalPipelineContainerInternal>> readOnlyGlobalPipelineMap;
    private bool isSealed = false;
    private ISpace space;

    public static ISpace Space { get; private set; }

    // New: lifetime from SpaceRegistry to choose specialized entries
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Scoped;

    private sealed class GlobalPipelineContainerInternal
    {
        internal GlobalPipelineConfig Config { get; }
        internal object Invoker { get; } // PipelineInvoker<TRequest, TResponse>
        
        internal GlobalPipelineContainerInternal(GlobalPipelineConfig config, object invoker)
        {
            Config = config;
            Invoker = invoker;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is not GlobalPipelineContainerInternal other)
                return false;
            
            if (Config.Order != other.Config.Order || Config.ExecutionStage != other.Config.ExecutionStage)
                return false;
            
            if (Invoker is Delegate thisDelegate && other.Invoker is Delegate otherDelegate)
            {
                return thisDelegate.Method == otherDelegate.Method && 
                       Equals(thisDelegate.Target, otherDelegate.Target);
            }
            
            return false;
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Config.Order;
                hash = hash * 23 + Config.ExecutionStage;
                
                if (Invoker is Delegate del)
                {
                    hash = hash * 23 + del.Method.GetHashCode();
                    if (del.Target != null)
                        hash = hash * 23 + del.Target.GetHashCode();
                }
                
                return hash;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static HandlerKey CreateKey<TRequest, TResponse>(string name)
        => new HandlerKey(typeof(TRequest), typeof(TResponse), name ?? string.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TypePairKey CreateTypePairKey<TRequest, TResponse>()
        => new TypePairKey(typeof(TRequest), typeof(TResponse));

    public void RegisterHandler<TRequest, TResponse>(
        HandlerInvoker<TRequest, TResponse> invoker,
        string name = "",
        IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelines = null,
        LightHandlerInvoker<TRequest, TResponse> lightInvoker = null)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more handlers can be registered.");

        var key = CreateKey<TRequest, TResponse>(name);
        var typeKey = CreateTypePairKey<TRequest, TResponse>();

        // Gather global pipelines for this (TRequest, TResponse) pair
        var globalPipelines = GetGlobalPipelinesForType<TRequest, TResponse>();

        object entryObj;
        // Choose specialized handler entry based on lifetime
        if (HandlerLifetime == ServiceLifetime.Singleton)
        {
            entryObj = new SpaceRegistry.SingletonHandlerEntry<TRequest, TResponse>(invoker, lightInvoker, pipelines, globalPipelines);
        }
        else
        {
            entryObj = new SpaceRegistry.ScopedHandlerEntry<TRequest, TResponse>(invoker, lightInvoker, pipelines, globalPipelines);
        }

        handlerMap[key] = entryObj;
        handlerMapByType[typeKey] = entryObj;
    }

    /// <summary>
    /// Register a pipeline-free handler using the optimized LightHandlerEntry.
    /// </summary>
    public void RegisterLightHandler<TRequest, TResponse>(
        HandlerInvoker<TRequest, TResponse> invoker,
        string name,
        LightHandlerInvoker<TRequest, TResponse> lightInvoker)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more handlers can be registered.");

        var key = CreateKey<TRequest, TResponse>(name);
        var typeKey = CreateTypePairKey<TRequest, TResponse>();

        var entryObj = new SpaceRegistry.LightHandlerEntry<TRequest, TResponse>(invoker, lightInvoker);

        handlerMap[key] = entryObj;
        handlerMapByType[typeKey] = entryObj;
    }

    /// <summary>
    /// Register a handler with exactly one pipeline using SinglePipelineEntry.
    /// </summary>
    public void RegisterSinglePipelineHandler<TRequest, TResponse>(
        HandlerInvoker<TRequest, TResponse> invoker,
        string name,
        PipelineInvoker<TRequest, TResponse> pipeline)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more handlers can be registered.");

        var key = CreateKey<TRequest, TResponse>(name);
        var typeKey = CreateTypePairKey<TRequest, TResponse>();

        var entryObj = new SpaceRegistry.SinglePipelineEntry<TRequest, TResponse>(invoker, pipeline);

        handlerMap[key] = entryObj;
        handlerMapByType[typeKey] = entryObj;
    }

    /// <summary>
    /// Register a handler with exactly two pipelines using TwoPipelinesEntry.
    /// </summary>
    public void RegisterTwoPipelinesHandler<TRequest, TResponse>(
        HandlerInvoker<TRequest, TResponse> invoker,
        string name,
        PipelineInvoker<TRequest, TResponse> pipeline1,
        PipelineInvoker<TRequest, TResponse> pipeline2)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more handlers can be registered.");

        var key = CreateKey<TRequest, TResponse>(name);
        var typeKey = CreateTypePairKey<TRequest, TResponse>();

        var entryObj = new SpaceRegistry.TwoPipelinesEntry<TRequest, TResponse>(invoker, pipeline1, pipeline2);

        handlerMap[key] = entryObj;
        handlerMapByType[typeKey] = entryObj;
    }

    /// <summary>
    /// Register a handler with exactly three pipelines using ThreePipelinesEntry.
    /// </summary>
    public void RegisterThreePipelinesHandler<TRequest, TResponse>(
        HandlerInvoker<TRequest, TResponse> invoker,
        string name,
        PipelineInvoker<TRequest, TResponse> pipeline1,
        PipelineInvoker<TRequest, TResponse> pipeline2,
        PipelineInvoker<TRequest, TResponse> pipeline3)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more handlers can be registered.");

        var key = CreateKey<TRequest, TResponse>(name);
        var typeKey = CreateTypePairKey<TRequest, TResponse>();

        var entryObj = new SpaceRegistry.ThreePipelinesEntry<TRequest, TResponse>(invoker, pipeline1, pipeline2, pipeline3);

        handlerMap[key] = entryObj;
        handlerMapByType[typeKey] = entryObj;
    }

    /// <summary>
    /// Register a handler with both handler pipelines and global pipelines.
    /// </summary>
    public void RegisterHandlerWithGlobalPipelines<TRequest, TResponse>(
        HandlerInvoker<TRequest, TResponse> invoker,
        string name,
        IEnumerable<(PipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> pipelines,
        IEnumerable<(GlobalPipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> globalPipelines)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more handlers can be registered.");

        var key = CreateKey<TRequest, TResponse>(name);
        var typeKey = CreateTypePairKey<TRequest, TResponse>();

        object entryObj;
        if (HandlerLifetime == ServiceLifetime.Singleton)
        {
            entryObj = new SpaceRegistry.SingletonHandlerEntry<TRequest, TResponse>(invoker, null, pipelines, globalPipelines);
        }
        else
        {
            entryObj = new SpaceRegistry.ScopedHandlerEntry<TRequest, TResponse>(invoker, null, pipelines, globalPipelines);
        }

        handlerMap[key] = entryObj;
        handlerMapByType[typeKey] = entryObj;
    }

    public void RegisterPipeline<TRequest, TResponse>(string handlerName, PipelineConfig pipelineConfig,
        PipelineInvoker<TRequest, TResponse> pipelineInvoker)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more pipelines can be registered.");

        var key = CreateKey<TRequest, TResponse>(handlerName);

        if (handlerMap.TryGetValue(key, out var handlerObj) && handlerObj is SpaceRegistry.HandlerEntry<TRequest, TResponse> entry)
        {
            entry.AddPipeline(pipelineInvoker, pipelineConfig);
        }
    }

    public void RegisterGlobalPipeline<TRequest, TResponse>(GlobalPipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)
    {
        if (isSealed)
            throw new InvalidOperationException("Registration is sealed. No more global pipelines can be registered.");

        var typeKey = CreateTypePairKey<TRequest, TResponse>();
        if (!globalPipelineMap.TryGetValue(typeKey, out var list))
        {
            list = [];
            globalPipelineMap[typeKey] = list;
        }

        list.Add(new GlobalPipelineContainerInternal(config, invoker));
    }

    private IEnumerable<(GlobalPipelineConfig config, PipelineInvoker<TRequest, TResponse> invoker)> GetGlobalPipelinesForType<TRequest, TResponse>()
    {
        var typeKey = CreateTypePairKey<TRequest, TResponse>();
        if (globalPipelineMap.TryGetValue(typeKey, out var list))
        {
            foreach (var gp in list)
            {
                yield return (gp.Config, (PipelineInvoker<TRequest, TResponse>)gp.Invoker);
            }
        }
    }

    public void CompleteRegistration()
    {
        if (!isSealed)
        {
            readOnlyHandlerMap = new ReadOnlyDictionary<HandlerKey, object>(handlerMap);
            readOnlyHandlerMapByType = new ReadOnlyDictionary<TypePairKey, object>(handlerMapByType);
            readOnlyGlobalPipelineMap = new ReadOnlyDictionary<TypePairKey, List<GlobalPipelineContainerInternal>>(globalPipelineMap);
            isSealed = true;

            handlerMap = null;
            handlerMapByType = null;
            globalPipelineMap = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetHandlerEntry<TRequest, TResponse>(string name, out SpaceRegistry.HandlerEntry<TRequest, TResponse> entry)
    {
        entry = null;

        if (!isSealed)
            return false;

        var key = CreateKey<TRequest, TResponse>(name);

        if (readOnlyHandlerMap != null && readOnlyHandlerMap.TryGetValue(key, out var obj) && obj is SpaceRegistry.HandlerEntry<TRequest, TResponse> he)
        {
            entry = he;
            return true;
        }

        // Fallback: try unnamed lookup
        if (!string.IsNullOrEmpty(name))
            return false;

        var typeKey = CreateTypePairKey<TRequest, TResponse>();
        if (readOnlyHandlerMapByType != null && readOnlyHandlerMapByType.TryGetValue(typeKey, out var byType) && byType is SpaceRegistry.HandlerEntry<TRequest, TResponse> he2)
        {
            entry = he2;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetHandlerEntryByRuntimeType(Type requestType, Type responseType, string name, out object entryObj)
    {
        entryObj = null;

        if (!isSealed)
            return false;

        if (string.IsNullOrEmpty(name))
        {
            // Prefer explicit unnamed mapping if present
            var key = new HandlerKey(requestType, responseType, string.Empty);
            if (readOnlyHandlerMap != null && readOnlyHandlerMap.TryGetValue(key, out var unnamed))
            {
                entryObj = unnamed;
                return true;
            }

            var typeKey = new TypePairKey(requestType, responseType);
            if (readOnlyHandlerMapByType != null && readOnlyHandlerMapByType.TryGetValue(typeKey, out var direct))
            {
                entryObj = direct;
                return true;
            }
        }
        else
        {
            var key = new HandlerKey(requestType, responseType, name);
            if (readOnlyHandlerMap != null && readOnlyHandlerMap.TryGetValue(key, out var named))
            {
                entryObj = named;
                return true;
            }
        }

        return false;
    }

    public ValueTask<TResponse> DispatchHandler<TRequest, TResponse>(IServiceProvider execProvider, HandlerContext<TRequest> ctx, string name = "")
    {
        if (!isSealed)
            throw new InvalidOperationException("Registration is not sealed. Call CompleteRegistration() before dispatching.");

        var key = CreateKey<TRequest, TResponse>(name);

        // Micro-optimization: cache ISpace instance once per registry
        space ??= execProvider.GetService<ISpace>();
        Space ??= space;

        if (readOnlyHandlerMap.TryGetValue(key, out var handlerObj) && handlerObj is SpaceRegistry.HandlerEntry<TRequest, TResponse> entry)
        {
            return entry.Invoke(ctx);
        }

        var typeKey = CreateTypePairKey<TRequest, TResponse>();
        if (readOnlyHandlerMapByType.TryGetValue(typeKey, out var typeHandlerObj) && typeHandlerObj is SpaceRegistry.HandlerEntry<TRequest, TResponse> typeEntry)
        {
            return typeEntry.Invoke(ctx);
        }

        // Fallback: enumerate to find the last registered handler
        SpaceRegistry.HandlerEntry<TRequest, TResponse> lastMatch = null;
        foreach (var kv in readOnlyHandlerMap)
        {
            if (kv.Key.RequestType == typeof(TRequest) && kv.Key.ResponseType == typeof(TResponse) && kv.Value is SpaceRegistry.HandlerEntry<TRequest, TResponse> e)
            {
                lastMatch = e;
            }
        }
        if (lastMatch != null)
        {
            return lastMatch.Invoke(ctx);
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
            if (responseType != null)
            {
                var key = new HandlerKey(type, responseType, string.Empty);
                if (readOnlyHandlerMap != null && readOnlyHandlerMap.TryGetValue(key, out var explicitUnnamed) && explicitUnnamed is SpaceRegistry.IObjectHandlerEntry explicitUnnamedEntry)
                {
                    var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
                    return explicitUnnamedEntry.InvokeObject(ctx);
                }

                var typeKey = new TypePairKey(type, responseType);
                if (readOnlyHandlerMapByType.TryGetValue(typeKey, out var handlerObjRt) && handlerObjRt is SpaceRegistry.IObjectHandlerEntry objectHandlerRt)
                {
                    var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
                    return objectHandlerRt.InvokeObject(ctx);
                }

                throw new InvalidOperationException($"Handler not found for type {type} -> {responseType}");
            }
            
            var objTypeKey = new TypePairKey(type, typeof(object));
            if (readOnlyHandlerMapByType.TryGetValue(objTypeKey, out var objHandler) && objHandler is SpaceRegistry.IObjectHandlerEntry objectHandler)
            {
                var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
                return objectHandler.InvokeObject(ctx);
            }

            // Fallback: try to find a single entry for the request type
            if (readOnlyHandlerMapByType != null)
            {
                int found = 0;
                SpaceRegistry.IObjectHandlerEntry last = null;

                foreach (var kv in readOnlyHandlerMapByType)
                {
                    if (kv.Key.RequestType == type && kv.Value is SpaceRegistry.IObjectHandlerEntry entry)
                    {
                        found++;
                        last = entry;
                        if (found > 1) break;
                    }
                }

                if (found == 1)
                {
                    var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
                    return last.InvokeObject(ctx);
                }
            }

            throw new InvalidOperationException($"Handler not found for type {type}");
        }

        if (responseType != null)
        {
            var key = new HandlerKey(type, responseType, name);
            if (readOnlyHandlerMap.TryGetValue(key, out var handlerObj2) && handlerObj2 is SpaceRegistry.IObjectHandlerEntry objectHandler2)
            {
                var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
                return objectHandler2.InvokeObject(ctx);
            }

            throw new InvalidOperationException($"Handler not found for type {type} -> {responseType} and name '{name}'");
        }

        // Legacy path
        foreach (var kv in readOnlyHandlerMap)
        {
            if (kv.Key.RequestType == type && kv.Key.Name == (name ?? string.Empty) && kv.Value is SpaceRegistry.IObjectHandlerEntry oh)
            {
                var ctx = HandlerContextStruct.Create(execProvider, request, space, ct);
                return oh.InvokeObject(ctx);
            }
        }

        throw new InvalidOperationException($"Handler not found for type {type} and name '{name}'");
    }
}
