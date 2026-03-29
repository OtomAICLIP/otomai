using System.Text.Json.Serialization;

namespace Bubble.Core.Datacenter.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DatacenterObjectAttribute : Attribute
{
    [JsonIgnore]
    public string Namespace { get; }

    [JsonIgnore]
    public string Class { get; }

    [JsonIgnore]
    public string Assembly { get; }

    [JsonIgnore]
    public string PrimaryKey { get; }

    public DatacenterObjectAttribute(string @namespace, string @class, string assembly, string primaryKey)
    {
        Namespace = @namespace;
        Class = @class;
        Assembly = assembly;
        PrimaryKey = primaryKey;
    }
}