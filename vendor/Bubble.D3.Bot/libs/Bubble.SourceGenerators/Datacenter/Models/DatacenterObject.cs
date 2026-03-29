using System.Collections.Immutable;

namespace Bubble.SourceGenerators.Datacenter.Models;

public sealed record DatacenterObject(
    string Namespace,
    string Name,
    string ObjectNamespace,
    string ObjectAssembly,
    string ObjectName,
    string ObjectPrimaryKey,
    bool HasBaseType,
    bool IsSealed,
    ImmutableArray<DatacenterProperty> Properties,
    ImmutableArray<string> BasePropertiesNames);