namespace Bubble.Core.Database.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TableAttribute : Attribute
{
    public string Name { get; }

    public DatabaseTypes DatabaseType { get; }

    public TableAttribute(string name, DatabaseTypes databaseType)
    {
        Name = name;
        DatabaseType = databaseType;
    }
}