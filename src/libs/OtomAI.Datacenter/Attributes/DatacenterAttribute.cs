namespace OtomAI.Datacenter.Attributes;

/// <summary>
/// Marks a class as a datacenter model loaded from Unity asset bundles.
/// Mirrors Bubble.Core.Datacenter's attribute-based mapping system.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DatacenterObjectAttribute : Attribute
{
    public string Module { get; }
    public DatacenterObjectAttribute(string module) => Module = module;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class DatacenterFieldAttribute : Attribute
{
    public string? Name { get; }
    public DatacenterFieldAttribute(string? name = null) => Name = name;
}
