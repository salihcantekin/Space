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
    public static void Generate(SourceProductionContext spc, HandlersCompileWrapperModel model)
    {
        var t = Template.Parse(EmbeddedResource.GetContent(dependencyInjectionFilePath), dependencyInjectionFilePath);
        var text = t.Render(model, member => member.Name);

        spc.AddSource("DependencyInjectionExtensions.g.cs", SourceText.From(text, Encoding.UTF8));
    }
}
