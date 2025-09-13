using Microsoft.CodeAnalysis;
using Space.SourceGenerator.Compile;
using Space.SourceGenerator.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Space.SourceGenerator.Scanning;

internal static class ModuleScanners
{
    internal record ModuleScanningCompileModelContainer
    {
        public List<ModuleCompileModel> Models { get; set; } = [];

        public string[] UniqueModuleAttributeNames
        {
            get
            {
                return [.. Models
                    .Select(m => m.ModuleName)
                    .Distinct()];
            }
        }
    }

    public static IncrementalValuesProvider<IMethodSymbol> GetModuleProvider(IncrementalGeneratorInitializationContext context)
    {
        var moduleAttributeMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax,
                transform: (ctx, _) =>
                {
                    return ctx.Node is not Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax methodSyntax
                        ? null
                        : ctx.SemanticModel.GetDeclaredSymbol(methodSyntax) is not IMethodSymbol methodSymbol ? null : methodSymbol;
                })
            .Where(x => x != null);

        return moduleAttributeMethods;
    }

    public static IEnumerable<ModuleCompileModel> ScanModules(IMethodSymbol methodSymbol)
    {
        var moduleAttributes = methodSymbol.GetAttributes()
                        .Where(attr =>
                            attr.AttributeClass != null &&
                            attr.AttributeClass.AllInterfaces.Any(i => i.Name == "ISpaceModuleAttribute") &&
                            attr.AttributeClass.BaseType?.Name == "Attribute");

        return moduleAttributes.Select(i => GetModuleModel(i, methodSymbol)).Where(i => i is not null);
    }

    private static ModuleCompileModel GetModuleModel(AttributeData attr, IMethodSymbol methodSymbol)
    {
        if (attr is null || methodSymbol is null)
            return null;

        // Only read explicit Profile named property; leave empty if not provided
        string profile = attr.GetAttributePropertyValue("Profile")?.ToString() ?? string.Empty;

        return new ModuleCompileModel
        {
            ClassFullName = methodSymbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MethodName = methodSymbol.Name,
            RequestType = GetRequestType(methodSymbol),
            ResponseType = methodSymbol.GetResponseGenericTypeName(fullName: true),
            ModuleName = attr.AttributeClass?.Name ?? "UnknownModule",
            ModuleProperties = attr.GetProperties(),
            ModulePropertiesLiterals = attr.GetPropertiesAsLiterals(),
            ModuleProviderType = attr.GetAttributePropertyValue("ModuleProviderType")?.ToString() ?? "",
            Profile = profile
        };
    }


    private static string GetRequestType(IMethodSymbol methodSymbol)
    {
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
}
