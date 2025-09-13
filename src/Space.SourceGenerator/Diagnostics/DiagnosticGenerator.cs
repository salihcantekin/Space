using Microsoft.CodeAnalysis;
using Space.SourceGenerator.Compile;
using Space.SourceGenerator.Diagnostics.Rules;
using System.Collections.Generic;

namespace Space.SourceGenerator.Diagnostics;

internal class DiagnosticGenerator
{
    // List of all diagnostic rules to be executed
    private static readonly List<IDiagnosticRule> rules =
    [
        // Handler rules
        new HandleAttributeRule(),
        new MissingHandlerAttributeRule(),
        new MultipleDefaultHandleRule(),

        // Notification rules
        new MissingNotificationAttributeRule(),
        new NotificationAttributeRule(),

        new MissingPipelineAttributeRule(),
        new PipelineAttributeRule()
        // SEND001 disabled for now (kept in code for future use)
        // new SendGenericResponseMismatchRule()
    ];

    internal static bool ReportDiagnostics(SourceProductionContext spc, Compilation compilation, HandlersCompileWrapperModel model)
    {
        bool hasErrors = false;

        foreach (var rule in rules)
        {
            var _hasError = rule.Analyze(spc, compilation, model);
            hasErrors = hasErrors || _hasError;
        }

        return hasErrors;
    }
}

