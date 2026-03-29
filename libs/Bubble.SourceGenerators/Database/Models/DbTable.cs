#nullable disable

namespace Bubble.SourceGenerators.Database.Models;

public sealed class DbTable
{
    public string Namespace { get; set; }

    public string Name { get; set; }

    public string IdentifiableName { get; set; }

    public string DbName { get; set; }

    public bool IsSealed { get; set; }

    public DbTypes DbType { get; set; }

    public string SequenceName { get; set; }

    public IEnumerable<DbColumn> Columns { get; set; }
}