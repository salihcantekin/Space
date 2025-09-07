using Microsoft.CodeAnalysis;
using Space.SourceGenerator.Compile;
using Space.SourceGenerator.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Space.SourceGenerator.Scanning;

internal static class NotificationScanner
{
    private static string GetHandlerRequestType(AttributeData notificationAttr, IMethodSymbol methodSymbol)
    {
        foreach (var param in methodSymbol.Parameters)
        {
            var paramType = param.Type;

            if (paramType is
                INamedTypeSymbol { Name: SourceGenConstants.Context.NotificationName, TypeArguments.Length: 1 } namedType)
            {
                return namedType.TypeArguments.First().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        return string.Empty;
    }

    public static IEnumerable<NotificationCompileModel> ScanHandlers(INamedTypeSymbol classSymbol,
        IMethodSymbol methodSymbol)
    {
        var handleAttr = methodSymbol.GetNotificationAttribute();
        if (handleAttr is null)
            yield break;

        var respType = GetHandlerRequestType(handleAttr, methodSymbol);

        yield return new NotificationCompileModel
        {
            ClassFullName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MethodName = methodSymbol.Name,
            RequestParameterTypeName = respType
        };
    }
}