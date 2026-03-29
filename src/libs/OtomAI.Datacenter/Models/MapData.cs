namespace OtomAI.Datacenter.Models;

/// <summary>
/// Dofus 3.0 map data. 560 cells per map, extracted from Unity asset bundles.
/// </summary>
public sealed class MapData
{
    public required long Id { get; init; }
    public required Cell[] Cells { get; init; }
    public InteractiveElement[] InteractiveElements { get; init; } = [];
    public AdjacentMap[] AdjacentMaps { get; init; } = [];
}

/// <summary>
/// Single cell on a map (560 per map).
/// LinkedZoneRp = (LinkedZone & 0xF0) >> 4
/// </summary>
public sealed class Cell
{
    public required int Id { get; init; }
    public int Speed { get; init; }
    public int LinkedZone { get; init; }
    public bool Walkable { get; init; }
    public bool LineOfSight { get; init; }
    public bool NonWalkableDuringFight { get; init; }
    public bool FarmCell { get; init; }

    public int LinkedZoneRp => (LinkedZone & 0xF0) >> 4;
}

public sealed class InteractiveElement
{
    public required int Id { get; init; }
    public required int TypeId { get; init; }
    public int CellId { get; init; }
}

public sealed class AdjacentMap
{
    public required long MapId { get; init; }
    public required int Direction { get; init; }
}

/// <summary>
/// World graph edge for inter-map pathfinding.
/// </summary>
public sealed class WorldGraphEdge
{
    public required WorldGraphNode From { get; init; }
    public required WorldGraphNode To { get; init; }
}

public sealed class WorldGraphNode
{
    public required long MapId { get; init; }
    public required int ZoneId { get; init; }
}
