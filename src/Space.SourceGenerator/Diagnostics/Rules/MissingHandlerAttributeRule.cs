using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Space.SourceGenerator.Compile;
using System.Linq;

namespace Space.SourceGenerator.Diagnostics.Rules;

/// <summary>
/// Diagnostic rule that checks for missing HandleAttribute on methods named 'Handle' in classes implementing IHandler&lt;TRequest, TResponse&gt;.
/// If a method named 'Handle' exists in a class implementing IHandler, but does not have the HandleAttribute, a diagnostic is reported.
/// </summary>
public class MissingHandlerAttributeRule : IDiagnosticRule
{
    /// <summary>
    /// Analyzes the compilation for classes implementing IHandler and reports diagnostics for 'Handle' methods missing the HandleAttribute.
    /// </summary>
    /// <param name="context">The source production context for reporting diagnostics.</param>
    /// <param name="compilation">The Roslyn compilation to analyze.</param>
    /// <param name="_">Unused model parameter (for interface compatibility).</param>
    public bool Analyze(SourceProductionContext context, Compilation compilation, HandlersCompileWrapperModel _)
    {
        bool hasErrors = false;
        foreach (var tree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            var classNodes = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classNode in classNodes)
            {
                if (semanticModel.GetDeclaredSymbol(classNode) is not INamedTypeSymbol classSymbol)
                    continue;

                var iHandlerInterfaces = classSymbol.Interfaces
                    .Where(i => i.Name == SourceGenConstants.Contracts.IHandler && i.TypeArguments.Length == 2);

                foreach (var iHandlerInterface in iHandlerInterfaces)
                {
                    foreach (var methodNode in classNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
                    {
                        if (semanticModel.GetDeclaredSymbol(methodNode) is not IMethodSymbol methodSymbol)
                            continue;

                        if (methodSymbol.Name != SourceGenConstants.Contracts.HandleMethodName)
                            continue;

                        // Attribute check
                        var hasHandleAttribute = methodSymbol.GetAttributes()
                            .Any(attr => attr.AttributeClass?.ToDisplayString() == SourceGenConstants.HandleAttributeFullName);

                        if (!hasHandleAttribute)
                        {
                            var diagnostic = Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    SourceGenConstants.InvalidHandlerFormatDiagnosticId,
                                    "Missing HandleAttribute",
                                    $"Method '{methodSymbol.Name}' must have {SourceGenConstants.HandleAttributeFullName}",
                                    "Usage",
                                    DiagnosticSeverity.Error,
                                    true),
                                methodNode.GetLocation()
                            );
                            context.ReportDiagnostic(diagnostic);

                            hasErrors = true;
                        }
                    }
                }
            }
        }

        return hasErrors;
    }
}
