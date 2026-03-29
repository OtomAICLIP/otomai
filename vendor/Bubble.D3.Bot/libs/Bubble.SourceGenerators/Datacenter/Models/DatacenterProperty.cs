namespace Bubble.SourceGenerators.Datacenter.Models;

public sealed record DatacenterProperty(
    string Name,
    string Type,
    string MethodName,
    bool IsText,
    bool IsList,
    bool IsListOfList,
    DatacenterProperty? LinkedProperty,
    string? SpecialType);