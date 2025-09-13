using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Space.SourceGenerator.Compile;
using System.Linq;

namespace Space.SourceGenerator.Diagnostics.Rules;

/// <summary>
/// Reports an error when calling ISpace.Send&lt;TRequest, TResponse&gt;(...) with a TResponse
/// that does not match the IRequest&lt;TRight&gt; implemented by TRequest AND there is no handler
/// registered for (TRequest, TResponse). This preserves scenarios where a request intentionally
/// supports multiple response types via different handlers.
/// </summary>
public sealed class SendGenericResponseMismatchRule : IDiagnosticRule
{
    private const string DiagnosticId = "SEND001";
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Send<TRequest,TResponse> generic response type mismatch",
        messageFormat: "TRequest '{0}' implements IRequest<{1}>, but Send is called with TResponse '{2}' and no handler exists for this pair. Use Send<{0}, {1}> or add a handler for the requested response type.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public bool Analyze(SourceProductionContext context, Compilation compilation, HandlersCompileWrapperModel model)
    {
        bool hasErrors = false;

        var irequestSymbol = compilation.GetTypeByMetadataName("Space.Abstraction.Contracts.IRequest`1");
        var ispaceSymbol = compilation.GetTypeByMetadataName("Space.Abstraction.ISpace");
        if (irequestSymbol is null || ispaceSymbol is null)
            return false; // types not available, skip

        foreach (var tree in compilation.SyntaxTrees)
        {
            var semantic = compilation.GetSemanticModel(tree);
            var invocations = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var inv in invocations)
            {
                var symbolInfo = semantic.GetSymbolInfo(inv);
                if (symbolInfo.Symbol is not IMethodSymbol method)
                    continue;

                if (method.Name != "Send" || method.Arity != 2)
                    continue;

                // Ensure it's a method on ISpace (or implementation thereof)
                var container = method.ContainingType;
                if (container is null)
                    continue;

                var isOnISpace = SymbolEqualityComparer.Default.Equals(container, ispaceSymbol) ||
                                 container.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, ispaceSymbol));
                if (!isOnISpace)
                    continue;

                var tReq = method.TypeArguments[0];
                var tRes = method.TypeArguments[1];

                // Check if TRequest implements IRequest<TRight>
                var reqIface = tReq?.AllInterfaces.FirstOrDefault(i =>
                    SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, irequestSymbol));
                if (reqIface is null)
                    continue; // no constraint to enforce

                var right = reqIface.TypeArguments.FirstOrDefault();
                if (right is null)
                    continue;

                if (!SymbolEqualityComparer.Default.Equals(right, tRes))
                {
                    // Allow when a handler exists explicitly for (TRequest,TResponse)
                    var reqName = tReq.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var resName = tRes.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    bool hasMatchingHandler = model?.Handlers?.Any(h =>
                        string.Equals(h.RequestParameterTypeName, reqName, System.StringComparison.Ordinal) &&
                        string.Equals(h.ReturnTypeName, resName, System.StringComparison.Ordinal)) == true;

                    if (hasMatchingHandler)
                        continue; // valid scenario

                    // Report at the type argument list if possible, else the invocation
                    Location loc = inv.GetLocation();
                    if (inv.Expression is GenericNameSyntax gns)
                        loc = gns.TypeArgumentList.GetLocation();

                    context.ReportDiagnostic(Diagnostic.Create(Rule, loc, tReq.ToDisplayString(), right.ToDisplayString(), tRes.ToDisplayString()));
                    hasErrors = true;
                }
            }
        }

        return hasErrors;
    }
}
