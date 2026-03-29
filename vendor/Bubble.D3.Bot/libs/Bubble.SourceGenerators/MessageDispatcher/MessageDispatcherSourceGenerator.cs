using Bubble.SourceGenerators.Infrastructure.Extensions;
using Bubble.SourceGenerators.MessageDispatcher.Attributes;
using Microsoft.CodeAnalysis;

namespace Bubble.SourceGenerators.MessageDispatcher;

[Generator(LanguageNames.CSharp)]
public sealed partial class MessageDispatcherSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(MessageHandlerAttribute.GlobalName, MessageHandlerAttribute.Source);
        });

        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform).Collect();

        var enabled = context.AnalyzerConfigOptionsProvider.IsEnabled("MessageHandler");

        var provider = syntaxProvider.Combine(enabled);

        context.RegisterSourceOutput(provider, (spc, pair) => Generate(spc, pair.Left, pair.Right));
    }
}