#nullable disable

namespace Bubble.SourceGenerators.Services.Models;

public sealed class Service
{
    public string Name { get; set; }

    public bool IsAsync { get; set; }

    public int Priority { get; set; }
}