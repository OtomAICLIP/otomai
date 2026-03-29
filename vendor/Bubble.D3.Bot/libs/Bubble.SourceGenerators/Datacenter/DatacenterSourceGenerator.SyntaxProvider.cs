using System.Collections.Immutable;
using Bubble.SourceGenerators.Datacenter.Models;
using Bubble.SourceGenerators.Infrastructure.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bubble.SourceGenerators.Datacenter;

public sealed partial class DatacenterSourceGenerator
{
    private static string GetMethodName(string propertyType)
    {
        return propertyType switch
        {
            "byte" => "Byte",
            "sbyte" => "SByte",
            "bool" or "System.Boolean" => "Boolean",
            "char" or "System.Char" => "Char",
            "decimal" or "System.Decimal" => "Decimal",
            "double" or "System.Double" => "Double",
            "float" or "System.Single" => "Single",
            "int" or "System.Int32" => "Int32",
            "uint" or "System.UInt32" => "UInt32",
            "long" or "System.Int64" => "Int64",
            "ulong" or "System.UInt64" => "UInt64",
            "ushort" or "System.UInt16" => "UInt16",
            "short" or "System.Int16" => "Int16",
            "string" or "System.String" => "CountStringInt32Aligned",
            _ => string.Empty
        };
    }

    private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (syntaxNode is not ClassDeclarationSyntax declarationSyntax)
            return false;

        if (declarationSyntax.AttributeLists.Count is 0)
            return false;

        return declarationSyntax
            .AttributeLists
            .SelectMany(x => x.Attributes)
            .Any(x => x.Name.ToString() is "DatacenterObject");
    }

    private static DatacenterObject Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol originalSymbol)
            throw new Exception("Symbol not found.");

        var tableAttribute = originalSymbol
            .GetAttributes()
            .First(x => x.AttributeClass?.Name is "DatacenterObjectAttribute");

        var objectNamespace = tableAttribute.ConstructorArguments[0].Value!.ToString();
        var objectName = tableAttribute.ConstructorArguments[1].Value!.ToString();
        var objectAssembly = tableAttribute.ConstructorArguments[2].Value!.ToString();
        var objectPrimaryKey = tableAttribute.ConstructorArguments[3].Value!.ToString();

        var @namespace = originalSymbol.ContainingNamespace.ToDisplayString();
        var name = originalSymbol.Name;

        var hasBaseType = originalSymbol.BaseType is not null && originalSymbol.BaseType.Name is not "Object";
        var isSealed = originalSymbol.IsSealed;

        var datacenterProperties = new List<DatacenterProperty>();

        var basePropertiesNames = new List<string>();

        var properties = originalSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x => x is { Kind: SymbolKind.Property, DeclaredAccessibility: Accessibility.Public, IsStatic: false, IsRequired: true } && !x.Name.StartsWith("get_") &&
                        !x.Name.StartsWith("set_"))
            .ToArray();

        foreach (var property in properties)
        {
            var attributes = property.GetAttributes();

            if (attributes.Any(x => x.AttributeClass?.Name is "DatacenterPropertyIgnoreAttribute"))
                continue;

            var linkedAttribute = attributes.FirstOrDefault(x => x.AttributeClass?.Name is "DatacenterPropertyLinkedAttribute");

            var linkedProperty = linkedAttribute is not null
                ? new DatacenterProperty(
                    linkedAttribute.ConstructorArguments[0].Value!.ToString(),
                    linkedAttribute.AttributeClass!.TypeArguments[0].ToString(),
                    string.Empty,
                    false,
                    false,
                    false,
                    null,
                    null)
                : null;

            var propertyName = property.Name;
            var propertyType = property.Type.ToDisplayString();

            var specialType = attributes
                .FirstOrDefault(x => x.AttributeClass?.Name.Contains("DatacenterPropertyAsAttribute") ?? false)
                ?.AttributeClass?.TypeArguments[0]
                .ToDisplayString();

            if (specialType is not null)
            {
                propertyType = specialType;
                specialType = property.Type.ToDisplayString();
            }

            var isText = attributes.Any(x => x.AttributeClass?.Name is "DatacenterPropertyTextAttribute");
            var isListOfList = propertyType.CountOccurrencesOfName("List") is 2;
            var isList = propertyType.CountOccurrencesOfName("List") is 1 && !isListOfList;

            if (isListOfList)
                if (property.Type is INamedTypeSymbol namedProperty && namedProperty.TypeArguments[0] is INamedTypeSymbol namedSubProperty &&
                    namedSubProperty.TypeArguments[0] is INamedTypeSymbol namedSubSubProperty)
                    propertyType = namedSubSubProperty.ToDisplayString();

            if (isList)
                if (property.Type is INamedTypeSymbol namedProperty && namedProperty.TypeArguments[0] is INamedTypeSymbol namedSubProperty)
                    propertyType = namedSubProperty.ToDisplayString();

            var methodName = GetMethodName(specialType ?? propertyType);

            datacenterProperties.Add(new DatacenterProperty(
                propertyName,
                propertyType,
                methodName,
                isText,
                isList,
                isListOfList,
                linkedProperty,
                specialType));
        }

        for (var symbol = originalSymbol.BaseType; symbol is not null && symbol.Name is not "Object"; symbol = symbol.BaseType)
            basePropertiesNames.AddRange(symbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(x => x is
                {
                    Kind: SymbolKind.Property, DeclaredAccessibility: Accessibility.Public, IsStatic: false, IsRequired: true, SetMethod.DeclaredAccessibility: Accessibility.Public
                } && !x.Name.StartsWith("get_") && !x.Name.StartsWith("set_"))
                .Where(x => !x.GetAttributes().Any(a => a.AttributeClass?.Name is "DatacenterPropertyIgnoreAttribute"))
                .Select(x => x.Name));

        return new DatacenterObject(
            @namespace,
            name,
            objectNamespace,
            objectAssembly,
            objectName,
            objectPrimaryKey,
            hasBaseType,
            isSealed,
            datacenterProperties.ToImmutableArray(),
            basePropertiesNames.ToImmutableArray());
    }
}