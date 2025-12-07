using System.Collections.Generic;
using System.Linq;

namespace Space.SourceGenerator.Compile;

internal static class ObjectNameExtensions
{
    internal static string Globalize(this string objectName)
    {
        return string.IsNullOrEmpty(objectName)
            ? objectName
            : objectName.StartsWith("global::")
                ? objectName
                : $"global::{objectName}";
    }
}

public class HandlersCompileWrapperModel
{
    private readonly HashSet<HandlersCompileModel> handlerCompileModels = [];
    private readonly HashSet<PipelineCompileModel> pipelineCompileModels = [];
    private readonly HashSet<GlobalPipelineCompileModel> globalPipelineCompileModels = [];
    private readonly HashSet<NotificationCompileModel> notificationCompileModels = [];
    private readonly HashSet<ModuleCompileModel> moduleCompileModels = [];

    // Indicates whether the root aggregator flag was explicitly provided via MSBuild property
    public bool HasExplicitRootFlag { get; set; }

    // Set by generator per-compilation
    public bool IsRootAggregator { get; set; }
    public string AssemblyName { get; set; }

    public HashSet<HandlersCompileModel> Handlers => handlerCompileModels;

    public HashSet<NotificationCompileModel> Notifications => notificationCompileModels;

    public HashSet<ModuleCompileModel> ModuleCompileModels => moduleCompileModels;
    
    public HashSet<GlobalPipelineCompileModel> GlobalPipelineCompileModels => globalPipelineCompileModels;
    
    public IEnumerable<ModuleProviderCompileModel> ModuleProviders => ModuleCompileModels.Select(i => new ModuleProviderCompileModel(i.ModuleName)).Distinct();


    public string[] HandlerClassNames { get; private set; }
    public string[] PipelineClassNames { get; private set; }
    public string[] GlobalPipelineClassNames { get; private set; }
    public string[] NotificationClassNames { get; private set; }

    public HashSet<string> AllHandlersName { get; private set; }

    // Ordered handlers for registration (default handlers last per Req/Res)
    public List<HandlersCompileModel> OrderedHandlers { get; private set; }

    // Standalone global pipelines (not attached to any specific handler class)
    public List<GlobalPipelineCompileModel> StandaloneGlobalPipelines { get; private set; }

    // Helper usage flags consumed by Scriban templates to suppress generation of unused locals (avoid CS8321 warnings)
    public bool NeedsReg { get; private set; }
    public bool NeedsRegLight { get; private set; }
    public bool NeedsRegPipe { get; private set; }
    public bool NeedsRegGlobalPipeline { get; private set; }
    public bool NeedsRegModule { get; private set; }
    public bool NeedsRegNotification { get; private set; }
    public bool NeedsVT { get; private set; }

    public void AddRangeHandlerAttribute(IEnumerable<HandlersCompileModel> models)
    {
        foreach (var model in models)
        {
            handlerCompileModels.Add(model);
        }
    }

    public void AddRangeModuleAttribute(IEnumerable<ModuleCompileModel> models)
    {
        foreach (var model in models)
        {
            AddModuleAttribute(model);
        }
    }

    public void AddModuleAttribute(ModuleCompileModel model)
    {
        moduleCompileModels.Add(model);
    }

    public void AddRangePipelineAttribute(IEnumerable<PipelineCompileModel> models)
    {
        foreach (var model in models)
        {
            pipelineCompileModels.Add(model);
        }
    }

    public void AddRangeGlobalPipelineAttribute(IEnumerable<GlobalPipelineCompileModel> models)
    {
        foreach (var model in models)
        {
            // Manual deduplication using key fields since record Equals includes all properties
            var isDuplicate = globalPipelineCompileModels.Any(existing =>
                existing.ClassFullName == model.ClassFullName &&
                existing.MethodName == model.MethodName &&
                existing.RequestParameterTypeName == model.RequestParameterTypeName &&
                existing.ReturnTypeName == model.ReturnTypeName &&
                existing.Order == model.Order &&
                existing.ExecutionStage == model.ExecutionStage);
            
            if (!isDuplicate)
            {
                globalPipelineCompileModels.Add(model);
            }
        }
    }

    public void AddRangeNotificationAttribute(IEnumerable<NotificationCompileModel> models)
    {
        foreach (var model in models)
        {
            notificationCompileModels.Add(model);
        }
    }

    public HandlersCompileWrapperModel Build()
    {
        // Attach pipelines + modules to handlers
        foreach (var handlerCompileModel in handlerCompileModels)
        {
            var pipelineNameMatch = pipelineCompileModels
                .Where(p => p.HandlerName == handlerCompileModel.HandlerName);

            var pipelineTypeMatch = pipelineCompileModels
                .Where(p => p.ReturnTypeName == handlerCompileModel.ReturnTypeName
                         && p.RequestParameterTypeName == handlerCompileModel.RequestParameterTypeName);

            var pipelineAllMatches = pipelineNameMatch
                .Union(pipelineTypeMatch)
                .Distinct()
                .OrderBy(i => i.Order); // Order by the pipeline order

            handlerCompileModel.PipelineCompileModels = [.. pipelineAllMatches];

            // Check if there are global pipelines for this handler's request/response type
            // We don't attach them to the handler model (they're registered centrally),
            // but we need to know if they exist to decide between Reg and RegLight
            var globalPipelineMatches = globalPipelineCompileModels
                .Where(gp => gp.ReturnTypeName == handlerCompileModel.ReturnTypeName
                          && gp.RequestParameterTypeName == handlerCompileModel.RequestParameterTypeName)
                .ToList();

            // Store in GlobalPipelineCompileModels so template can check .size
            handlerCompileModel.GlobalPipelineCompileModels = [.. globalPipelineMatches];

            var moduleTypeMatch = moduleCompileModels
                .Where(c => (c.ResponseType.Contains(handlerCompileModel.ReturnTypeName) || handlerCompileModel.ReturnTypeName.Contains(c.ResponseType)) &&
                            c.RequestType == handlerCompileModel.RequestParameterTypeName &&
                            c.MethodName == handlerCompileModel.MethodName &&
                            c.ClassFullName == handlerCompileModel.ClassFullName);

            handlerCompileModel.ModuleCompileModels = [.. moduleTypeMatch];
        }

        HandlerClassNames = [.. handlerCompileModels
            .Select(m => m.ClassFullName.Globalize())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()];

        PipelineClassNames = [.. pipelineCompileModels
            .Select(m => m.ClassFullName.Globalize())
            .Except(HandlerClassNames)
            .Distinct()]; // they might be in the same class

        GlobalPipelineClassNames = [.. globalPipelineCompileModels
            .Select(m => m.ClassFullName.Globalize())
            .Except(HandlerClassNames)
            .Except(PipelineClassNames)
            .Distinct()];

        NotificationClassNames = [.. notificationCompileModels
            .Select(i => i.ClassFullName.Globalize())
            .Except(HandlerClassNames)
            .Except(PipelineClassNames)
            .Except(GlobalPipelineClassNames)
            .Distinct()];

        AllHandlersName = [.. HandlerClassNames
                                .Union(PipelineClassNames)
                                .Union(GlobalPipelineClassNames)
                                .Union(NotificationClassNames)];

        // Compute ordered handlers: non-default first, then default within each (Req, Res)
        OrderedHandlers = [.. handlerCompileModels
            .GroupBy(h => (h.RequestParameterTypeName, h.ReturnTypeName))
            .SelectMany(g => g.OrderBy(h => h.IsDefault ? 1 : 0))];

        // Standalone global pipelines are no longer needed - all global pipelines
        // will be registered centrally via GlobalPipelineCompileModels
        StandaloneGlobalPipelines = [];

        // Flags: use per-handler attachment, not global collection counts, to avoid unused helper generation
        NeedsRegNotification = notificationCompileModels.Count > 0;
        NeedsRegPipe = handlerCompileModels.Any(h => h.PipelineCompileModels.Length > 0);
        NeedsRegGlobalPipeline = globalPipelineCompileModels.Count > 0;
        NeedsRegModule = handlerCompileModels.Any(h => h.ModuleCompileModels.Length > 0);
        
        // NeedsReg is true when handlers have pipelines, modules, OR global pipelines
        NeedsReg = handlerCompileModels.Any(h => h.PipelineCompileModels.Length > 0 || h.ModuleCompileModels.Length > 0 || h.GlobalPipelineCompileModels.Length > 0);
        
        // NeedsRegLight is true when ANY handler has no pipelines, no modules, and no global pipelines
        NeedsRegLight = handlerCompileModels.Any(h => h.PipelineCompileModels.Length == 0 && h.ModuleCompileModels.Length == 0 && h.GlobalPipelineCompileModels.Length == 0);
        
        NeedsVT = handlerCompileModels.Any(h => !h.IsValueTask) || pipelineCompileModels.Any(p => !p.IsValueTask) || globalPipelineCompileModels.Any(gp => !gp.IsValueTask);

        return this;
    }
}

