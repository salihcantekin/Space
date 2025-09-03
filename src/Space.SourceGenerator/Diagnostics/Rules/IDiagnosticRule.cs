using Microsoft.CodeAnalysis;
using Space.SourceGenerator.Compile;


// Represents a diagnostic rule that can analyze the model and report diagnostics.
public interface IDiagnosticRule
{
    bool Analyze(SourceProductionContext context, Compilation compilation, HandlersCompileWrapperModel model);
}