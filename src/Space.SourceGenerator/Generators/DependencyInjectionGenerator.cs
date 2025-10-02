using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using Space.SourceGenerator.Compile;
using Space.SourceGenerator.Helpers;
using System.Text;

namespace Space.SourceGenerator.Generators;

internal class DependencyInjectionGenerator
{
    private const string dependencyInjectionFilePath = "Resources/DependencyInjectionExtensions.scr";
    private const string assemblyRegistrationFilePath = "Resources/AssemblyRegistration.scr"; // new

    public static void Generate(SourceProductionContext spc, HandlersCompileWrapperModel model)
    {
        string templatePath = model.IsRootAggregator ? dependencyInjectionFilePath : assemblyRegistrationFilePath;
        var t = Template.Parse(EmbeddedResource.GetContent(templatePath), templatePath);
        var text = t.Render(model, member => member.Name);

        var hintName = model.IsRootAggregator ? "DependencyInjectionExtensions.g.cs" : $"SpaceAssemblyRegistration_{Sanitize(model.AssemblyName)}.g.cs";
        spc.AddSource(hintName, SourceText.From(text, Encoding.UTF8));
    }

    private static string Sanitize(string assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName)) return "Unknown";
        var sb = new StringBuilder(assemblyName.Length);
        foreach (var ch in assemblyName)
        {
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }
        return sb.ToString();
    }
}
