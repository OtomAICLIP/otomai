using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bubble.SourceGenerators.MessageFactory;

public sealed partial class TypeFactorySourceGenerator
{
    private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (syntaxNode is not ClassDeclarationSyntax declarationSyntax)
            return false;

        if (declarationSyntax.BaseList is null)
            return false;

        return declarationSyntax
            .BaseList
            .Types
            .Select(static x => x.Type)
            .Any(static x => x.ToString() is "IProtoMessage");
    }

    private static string Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol symbol)
            throw new Exception("Symbol not found.");

        return symbol.ToDisplayString();
    }
}