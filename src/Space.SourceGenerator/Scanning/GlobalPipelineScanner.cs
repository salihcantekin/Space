using Microsoft.CodeAnalysis;
using Space.SourceGenerator.Compile;
using Space.SourceGenerator.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Space.SourceGenerator.Scanning;

internal static class GlobalPipelineScanner
{
    private static string GetGlobalPipelineRequestType(IMethodSymbol methodSymbol)
    {
        // Global pipeline methods are generic: ValidateRequest<TRequest, TResponse>
        // Extract TRequest from generic type parameters
        if (methodSymbol.TypeParameters.Length >= 1)
        {
            // First type parameter is TRequest
            return methodSymbol.TypeParameters[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        // Fallback: Look for a parameter of type PipelineContext<T> and extract T
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

        return string.Empty;
    }

    private static (string, string, bool) GetGlobalPipelineResponseType(IMethodSymbol methodSymbol)
    {
        // Extract TResponse from generic type parameters
        if (methodSymbol.TypeParameters.Length >= 2)
        {
            // Second type parameter is TResponse
            var tResponse = methodSymbol.TypeParameters[1];
            var respTypeName = tResponse.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            
            // Check return type for Task/ValueTask wrapping
            var returnType = methodSymbol.ReturnType;
            if (returnType is INamedTypeSymbol namedReturn)
            {
                var taskName = namedReturn.Name;
                bool voidLike = (namedReturn.Name == SourceGenConstants.Type.Task || namedReturn.Name == SourceGenConstants.Type.ValueTask) && 
                               namedReturn.TypeArguments.Length == 0;
                
                return (taskName, respTypeName, voidLike);
            }
        }

        // Fallback to original implementation
        var taskNameFallback = methodSymbol.GetResponseTaskName();
        var respTypeNameFallback = methodSymbol.GetResponseGenericTypeName();
        bool voidLikeFallback = methodSymbol.ReturnType is INamedTypeSymbol r && 
                               (r.Name == SourceGenConstants.Type.Task || r.Name == SourceGenConstants.Type.ValueTask) && 
                               r.TypeArguments.Length == 0;
        return (taskNameFallback, respTypeNameFallback, voidLikeFallback);
    }

    public static IEnumerable<GlobalPipelineCompileModel> ScanGlobalPipelines(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol)
    {
        var globalPipelineAttr = methodSymbol.GetGlobalPipelineAttribute();
        if (globalPipelineAttr is null)
            yield break;

        // Check if method is generic (has TRequest, TResponse type parameters)
        bool isGeneric = methodSymbol.TypeParameters.Length >= 2;

        // nameof(GlobalPipelineAttribute.Order)
        var orderStr = globalPipelineAttr.GetAttributeArgument("Order");
        if (!int.TryParse(orderStr, out var order))
        {
            order = 100; // Default order if parsing fails
        }

        // nameof(GlobalPipelineAttribute.ExecutionStage)
        var executionStageStr = globalPipelineAttr.GetAttributeArgument("ExecutionStage");
        if (!int.TryParse(executionStageStr, out var executionStage))
        {
            executionStage = 0; // Default to BeforeHandler
        }

        var reqType = GetGlobalPipelineRequestType(methodSymbol);
        var (taskName, respType, voidLike) = GetGlobalPipelineResponseType(methodSymbol);

        yield return new GlobalPipelineCompileModel
        {
            ClassFullName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MethodName = methodSymbol.Name,
            ReturnTaskTypeName = taskName,
            RequestParameterTypeName = reqType,
            ReturnTypeName = respType,
            IsVoidLike = voidLike,
            IsGeneric = isGeneric,
            Order = order,
            ExecutionStage = executionStage,
            Properties = globalPipelineAttr.GetProperties()
        };
    }
}
