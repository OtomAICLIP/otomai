using Bubble.SourceGenerators.Infrastructure.Extensions;
using Microsoft.CodeAnalysis;

namespace Bubble.SourceGenerators.MessageFactory;

[Generator(LanguageNames.CSharp)]
public sealed partial class TypeFactorySourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform).Collect();

        var enabledProvider = context.AnalyzerConfigOptionsProvider.IsEnabled("MessageFactory");

        var provider = syntaxProvider.Combine(enabledProvider);

        context.RegisterSourceOutput(provider, static (spc, pair) => Generate(spc, pair.Left, pair.Right));
    }
}