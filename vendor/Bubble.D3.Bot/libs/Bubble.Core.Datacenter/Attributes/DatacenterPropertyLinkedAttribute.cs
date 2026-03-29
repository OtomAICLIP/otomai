namespace Bubble.Core.Datacenter.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DatacenterPropertyLinkedAttribute<T> : Attribute
    where T : class
{
    public string Name { get; }

    public DatacenterPropertyLinkedAttribute(string name)
    {
        Name = name;
    }
}