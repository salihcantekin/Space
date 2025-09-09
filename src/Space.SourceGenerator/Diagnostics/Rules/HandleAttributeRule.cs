using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Space.SourceGenerator.Compile;
using System.Linq;

namespace Space.SourceGenerator.Diagnostics.Rules;
// This rule checks for methods with HandleAttribute and validates their signature.
public class HandleAttributeRule : IDiagnosticRule
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

                foreach (var methodNode in classNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (semanticModel.GetDeclaredSymbol(methodNode) is not IMethodSymbol methodSymbol)
                        continue;


                    // Check if method has HandleAttribute
                    var hasHandleAttribute = methodSymbol.GetAttributes()
                        .Any(attr => attr.AttributeClass?.ToDisplayString() == SourceGenConstants.HandleAttributeFullName);

                    if (!hasHandleAttribute)
                        continue;

                    // Rule 1: Must have exactly one parameter
                    if (methodSymbol.Parameters.Length != 1)
                    {
                        ReportDiagnostic(context, methodNode, SourceGenConstants.HandleInvalidParameterCountDiagnosticId, SourceGenConstants.HandleInvalidParameterCountMessage);
                        hasErrors = true;
                        continue;
                    }

                    // Rule 2: Parameter must be HandlerContext<TRequest>
                    if (methodSymbol.Parameters[0].Type is not INamedTypeSymbol paramType || paramType.Name != SourceGenConstants.Context.HandlerName || paramType.TypeArguments.Length != 1)
                    {
                        ReportDiagnostic(context, methodNode, SourceGenConstants.HandleInvalidParameterTypeDiagnosticId, SourceGenConstants.HandleInvalidParameterTypeMessage);
                        hasErrors = true;
                        continue;
                    }

                    // Rule 3: Return type must be ValueTask<TResponse>
                    if (methodSymbol.ReturnType is not INamedTypeSymbol returnType || (returnType.Name != SourceGenConstants.Type.ValueTask && returnType.Name != SourceGenConstants.Type.Task) || returnType.TypeArguments.Length != 1)
                    {
                        ReportDiagnostic(context, methodNode, SourceGenConstants.HandleInvalidReturnTypeDiagnosticId, SourceGenConstants.HandleInvalidReturnTypeMessage);
                        hasErrors = true;
                        continue;
                    }

                    // Rule 4: Method must be public or internal
                    if (methodSymbol.DeclaredAccessibility != Accessibility.Public && methodSymbol.DeclaredAccessibility != Accessibility.Internal)
                    {
                        ReportDiagnostic(context, methodNode, SourceGenConstants.HandleInvalidAccessibilityDiagnosticId, SourceGenConstants.HandleInvalidAccessibilityMessage);
                        hasErrors = true;
                        continue;
                    }
                }
            }
        }

        return hasErrors;
    }

    private void ReportDiagnostic(SourceProductionContext context, MethodDeclarationSyntax methodNode, string id, string message)
    {
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(id, "Invalid Handle Method Signature", message, "Usage", DiagnosticSeverity.Error, true),
            methodNode.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }
}