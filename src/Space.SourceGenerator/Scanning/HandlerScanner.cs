using Microsoft.CodeAnalysis;
using Space.SourceGenerator.Compile;
using Space.SourceGenerator.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Space.SourceGenerator.Scanning;

internal static class HandlerScanner
{
    private static string GetHandlerRequestType(AttributeData handleAttr, IMethodSymbol methodSymbol)
    {
        // Look for a parameter of type HandlerContext<T> and extract T
        foreach (var param in methodSymbol.Parameters)
        {
            var paramType = param.Type;

            if (paramType is INamedTypeSymbol namedType &&
                namedType.Name == SourceGenConstants.Context.HandlerName &&
                namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments.First().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        return string.Empty;
    }

    // returns (taskName, responseTypeName) where taskName is the name of the task type (e.g., Task, ValueTask)
    // and responseTypeName is the type of the response (e.g., string, int, etc.)
    // either from HandleAttribute or from the method signature
    private static (string, string) GetHandlerResponseType(AttributeData handleAttr, IMethodSymbol methodSymbol)
    {
        var respTypeName = methodSymbol.GetResponseGenericTypeName();
        var taskName = methodSymbol.GetResponseTaskName();

        return (taskName, respTypeName);
    }

    public static IEnumerable<HandlersCompileModel> ScanHandlers(INamedTypeSymbol classSymbol,
        IMethodSymbol methodSymbol)
    {
        var handleAttr = methodSymbol.GetHandlerAttribute();
        if (handleAttr is null)
            yield break;

        var reqType = GetHandlerRequestType(handleAttr, methodSymbol);
        var (taskName, respType) = GetHandlerResponseType(handleAttr, methodSymbol);

        // nameof(HandleAttribute.Name)
        var model = new HandlersCompileModel(handleAttr.GetAttributeArgument("Name"))
        {
            ClassFullName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            RequestParameterTypeName = reqType,
            MethodName = methodSymbol.Name,
            ReturnTypeName = respType,
            ReturnTaskTypeName = taskName,
        };

        var isDefaultRaw = handleAttr.GetAttributeArgument("IsDefault");
        if (bool.TryParse(isDefaultRaw, out var isDef))
        {
            model.IsDefault = isDef;
        }

        yield return model;
    }
}