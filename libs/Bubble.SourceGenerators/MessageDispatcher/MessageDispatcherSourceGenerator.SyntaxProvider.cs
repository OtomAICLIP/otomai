using Bubble.SourceGenerators.MessageDispatcher.Attributes;
using Bubble.SourceGenerators.MessageDispatcher.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bubble.SourceGenerators.MessageDispatcher;

public sealed partial class MessageDispatcherSourceGenerator
{
    private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (syntaxNode is not MethodDeclarationSyntax declarationSyntax)
            return false;

        return declarationSyntax
            .AttributeLists
            .SelectMany(x => x.Attributes)
            .Any(x => x.Name.ToString() is MessageHandlerAttribute.Name);
    }

    private static MessageHandler Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not IMethodSymbol methodSymbol)
            throw new Exception("Method symbol not found.");

        var handlerTypeName = methodSymbol.ContainingSymbol.ToDisplayString();

        var handlerMethodName = methodSymbol.Name;

        var messageTypeName = methodSymbol.Parameters[1].Type.ToDisplayString();

        var type = methodSymbol.Parameters[1].Type.Name switch
        {
            var name when name.Contains("Request") => MessageTypes.Request,
            var name when name.Contains("Response") => MessageTypes.Response,
            var name when name.Contains("Event") => MessageTypes.Event,
            _ => MessageTypes.None
        };

        return new MessageHandler
        {
            HandlerTypeName = handlerTypeName,
            HandlerMethodName = handlerMethodName,
            MessageTypeName = messageTypeName,
            Type = type
        };
    }
}