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
    private readonly HashSet<NotificationCompileModel> notificationCompileModels = [];
    private readonly HashSet<ModuleCompileModel> moduleCompileModels = [];

    public HashSet<HandlersCompileModel> Handlers => handlerCompileModels;

    public HashSet<NotificationCompileModel> Notifications => notificationCompileModels;

    public HashSet<ModuleCompileModel> ModuleCompileModels => moduleCompileModels;
    public IEnumerable<ModuleProviderCompileModel> ModuleProviders => ModuleCompileModels.Select(i => new ModuleProviderCompileModel(i.ModuleName)).Distinct();


    public string[] HandlerClassNames { get; private set; }
    public string[] PipelineClassNames { get; private set; }
    public string[] NotificationClassNames { get; private set; }



    public HashSet<string> AllHandlersName { get; private set; }


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

    public void AddRangeNotificationAttribute(IEnumerable<NotificationCompileModel> models)
    {
        foreach (var model in models)
        {
            notificationCompileModels.Add(model);
        }
    }


    public HandlersCompileWrapperModel Build()
    {
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

            var moduleTypeMatch = moduleCompileModels
                .Where(c => (c.ResponseType.Contains(handlerCompileModel.ReturnTypeName) || handlerCompileModel.ReturnTypeName.Contains(c.ResponseType)) &&
                            c.RequestType == handlerCompileModel.RequestParameterTypeName);

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

        NotificationClassNames = [.. notificationCompileModels
            .Select(i => i.ClassFullName.Globalize())
            .Except(HandlerClassNames)
            .Except(PipelineClassNames)
            .Distinct()];


        AllHandlersName = [.. HandlerClassNames
                                .Union(PipelineClassNames)
                                .Union(NotificationClassNames)];

        return this;
    }
}

