using Bubble.SourceGenerators.Services.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bubble.SourceGenerators.Services;

public sealed partial class ServiceSourceGenerator
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
            .ToArray()
            .Any(t => t.Type.ToString().Contains("SingletonSetup") || t.Type.ToString().Contains("SingletonAsyncSetup"));
    }

    private static Service Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol symbol)
            throw new Exception("Symbol not found.");

        var name = symbol.ToDisplayString();

        var isAsync = symbol.BaseType?.Name.Contains("Async") ?? false;

        var priorityProperty = symbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .First(p => p.Name == "Priority");

        var syntaxRef = priorityProperty.DeclaringSyntaxReferences.First();

        var syntaxRaw = syntaxRef
            .SyntaxTree
            .ToString()
            .Substring(syntaxRef.Span.Start, syntaxRef.Span.End - syntaxRef.Span.Start)
            .Split(['>'], StringSplitOptions.RemoveEmptyEntries)
            .Last()
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Replace(";", string.Empty)
            .Trim();

        if (!int.TryParse(syntaxRaw, out var priority))
            priority = 0;

        return new Service
        {
            Name = name,
            Priority = priority,
            IsAsync = isAsync
        };
    }
}