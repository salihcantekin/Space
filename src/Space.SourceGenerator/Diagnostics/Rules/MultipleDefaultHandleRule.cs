using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Space.SourceGenerator.Compile;
using Space.SourceGenerator.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Space.SourceGenerator.Diagnostics.Rules;

public class MultipleDefaultHandleRule : IDiagnosticRule
{
    public bool Analyze(SourceProductionContext context, Compilation compilation, HandlersCompileWrapperModel _)
    {
        bool hasErrors = false;

        foreach (var tree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            var classNodes = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classNode in classNodes)
            {
                if (semanticModel.GetDeclaredSymbol(classNode) is not INamedTypeSymbol)
                    continue;

                var defaults = new List<(MethodDeclarationSyntax methodNode, string req, string res)>();

                foreach (var methodNode in classNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (semanticModel.GetDeclaredSymbol(methodNode) is not IMethodSymbol methodSymbol)
                        continue;

                    var handleAttr = methodSymbol.GetAttributes()
                        .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == SourceGenConstants.HandleAttributeFullName);

                    if (handleAttr is null)
                        continue;

                    bool isDefault = false;
                    foreach (var na in handleAttr.NamedArguments)
                    {
                        if (string.Equals(na.Key, "IsDefault", StringComparison.OrdinalIgnoreCase) && na.Value.Value is bool b)
                        {
                            isDefault = b; break;
                        }
                    }

                    if (!isDefault)
                        continue;

                    string reqType = string.Empty;
                    foreach (var p in methodSymbol.Parameters)
                    {
                        if (p.Type is INamedTypeSymbol nt && nt.Name == SourceGenConstants.Context.HandlerName && nt.TypeArguments.Length == 1)
                        {
                            reqType = nt.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(reqType))
                        continue;

                    string resType = methodSymbol.GetResponseGenericTypeName();
                    if (!string.IsNullOrEmpty(resType))
                    {
                        defaults.Add((methodNode, reqType, resType));
                    }
                }

                foreach (var g in defaults.GroupBy(d => (d.req, d.res)))
                {
                    if (g.Count() > 1)
                    {
                        hasErrors = true;
                        foreach (var d in g)
                        {
                            var diag = Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    id: "HANDLE020",
                                    title: "Conflicting Default Handlers",
                                    messageFormat: $"Multiple default handlers (IsDefault=true) found for {g.Key.req} -> {g.Key.res}. Only one default handler is allowed per request/response pair.",
                                    category: "Usage",
                                    defaultSeverity: DiagnosticSeverity.Error,
                                    isEnabledByDefault: true),
                                d.methodNode.GetLocation());

                            context.ReportDiagnostic(diag);
                        }
                    }
                }
            }
        }

        return hasErrors;
    }
}
