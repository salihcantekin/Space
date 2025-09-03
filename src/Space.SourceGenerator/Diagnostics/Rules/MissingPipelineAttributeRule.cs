using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Space.SourceGenerator.Compile;
using System.Linq;

namespace Space.SourceGenerator.Diagnostics.Rules;

/// <summary>
/// Diagnostic rule that checks for missing PipelineAttribute on methods named 'Invoke' in classes implementing IPipelineHandler<TRequest, TResponse>.
/// </summary>
public class MissingPipelineAttributeRule : IDiagnosticRule
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
                if (semanticModel.GetDeclaredSymbol(classNode) is not INamedTypeSymbol classSymbol)
                    continue;

                var iPipelineAttributes = classSymbol.Interfaces
                    .Where(i => i.Name == SourceGenConstants.Contracts.IPipelineHandler && i.TypeArguments.Length == 2);

                foreach (var iPipelineAttribute in iPipelineAttributes)
                {
                    foreach (var methodNode in classNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
                    {
                        if (semanticModel.GetDeclaredSymbol(methodNode) is not IMethodSymbol methodSymbol)
                            continue;

                        if (methodSymbol.Name != SourceGenConstants.Contracts.PipelineMethodName)
                            continue;

                        // Attribute check
                        var hasPipelineAttribute = methodSymbol.GetAttributes()
                            .Any(attr => attr.AttributeClass?.ToDisplayString() == SourceGenConstants.PipelineAttributeFullName);

                        if (!hasPipelineAttribute)
                        {
                            var diagnostic = Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "PIPELINE001",
                                    "Missing PipelineAttribute",
                                    $"Method '{methodSymbol.Name}' must have {SourceGenConstants.PipelineAttributeFullName}",
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
