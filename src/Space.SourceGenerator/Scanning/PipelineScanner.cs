using Microsoft.CodeAnalysis;
using Space.SourceGenerator.Compile;
using Space.SourceGenerator.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Space.SourceGenerator.Scanning;


internal static class PipelineScanner
{
    private static string GetPipelineRequestType(IMethodSymbol methodSymbol)
    {
        // Look for a parameter of type HandlerContext<T> and extract T
        foreach (var param in methodSymbol.Parameters)
        {
            var paramType = param.Type;

            if (paramType is INamedTypeSymbol namedType &&
                namedType.Name == SourceGenConstants.Context.PipelineName &&
                namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments.First().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        return "object";
    }

    private static (string, string, bool) GetPipelineResponseType(AttributeData pipelineAttr, IMethodSymbol methodSymbol)
    {
        var taskName = methodSymbol.GetResponseTaskName();
        var respTypeName = methodSymbol.GetResponseGenericTypeName();
        bool voidLike = methodSymbol.ReturnType is INamedTypeSymbol r && (r.Name == SourceGenConstants.Type.Task || r.Name == SourceGenConstants.Type.ValueTask) && r.TypeArguments.Length == 0;
        return (taskName, respTypeName, voidLike);
    }


    public static IEnumerable<PipelineCompileModel> ScanPipelines(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol)
    {
        var pipelineAttr = methodSymbol.GetPipelineAttribute();
        if (pipelineAttr is null)
            yield break;

        //nameof(PipelineAttribute.HandleName)
        var handlerName = pipelineAttr.GetAttributeArgument("HandleName");

        if (string.IsNullOrEmpty(handlerName) && pipelineAttr.ConstructorArguments.Length > 0)
        {
            handlerName = pipelineAttr.ConstructorArguments[0].Value?.ToString();
        }

        // nameof(PipelineAttribute.Order)
        var pipelineOrderStr = pipelineAttr.GetAttributeArgument("Order");
        if (!int.TryParse(pipelineOrderStr, out var order))
        {
            order = 100; // Default order if parsing fails
        }

        var reqType = GetPipelineRequestType(methodSymbol);
        var (taskName, respType, voidLike) = GetPipelineResponseType(pipelineAttr, methodSymbol);

        yield return new PipelineCompileModel
        {
            ClassFullName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MethodName = methodSymbol.Name,
            HandlerName = handlerName,
            ReturnTaskTypeName = taskName,
            RequestParameterTypeName = reqType,
            ReturnTypeName = respType,
            IsVoidLike = voidLike,
            Order = order,
            Properties = pipelineAttr.GetProperties()
        };
    }
}
