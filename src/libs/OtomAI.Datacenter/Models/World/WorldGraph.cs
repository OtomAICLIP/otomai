using OtomAI.Datacenter.Attributes;

namespace OtomAI.Datacenter.Models.World;

/// <summary>
/// World graph for inter-map pathfinding.
/// Mirrors Bubble.Core.Datacenter's WorldGraph model set:
/// Vertices (map + zone) connected by edges (transitions).
/// </summary>
[DatacenterObject("WorldGraph")]
public sealed class WorldGraphVertex
{
    public int Id { get; set; }
    public long MapId { get; set; }
    public int ZoneId { get; set; }
}

[DatacenterObject("WorldGraphEdges")]
public sealed class WorldGraphTransition
{
    public int Id { get; set; }
    public int FromVertexId { get; set; }
    public int ToVertexId { get; set; }
    public TransitionType Type { get; set; }
    public int Direction { get; set; }
    public int CellId { get; set; }
    public int SkillId { get; set; }
    public int CriterionId { get; set; }
}

public enum TransitionType
{
    Scroll = 0,
    ScrollAction = 1,
    MapAction = 2,
    Interactive = 3,
}

[DatacenterObject("SubAreas")]
public sealed class SubArea
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int AreaId { get; set; }
    public long[] MapIds { get; set; } = [];
    public int Level { get; set; }
    public bool IsConquestVillage { get; set; }
}

[DatacenterObject("Areas")]
public sealed class Area
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SuperAreaId { get; set; }
    public int WorldMapId { get; set; }
}
