using Bubble.SourceGenerators.Infrastructure.Extensions;
using Microsoft.CodeAnalysis;

namespace Bubble.SourceGenerators.Datacenter;

[Generator(LanguageNames.CSharp)]
public sealed partial class DatacenterSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform).Collect();

        var enabled = context.AnalyzerConfigOptionsProvider.IsEnabled("Datacenter");

        var provider = syntaxProvider.Combine(enabled);

        context.RegisterSourceOutput(provider, (spc, pair) => Generate(spc, pair.Left, pair.Right));
    }
}