namespace Bubble.Core.Datacenter.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DatacenterPropertyAsAttribute<T> : Attribute where T : struct;