using Serilog;

namespace OtomAI.Bot.Maps.World;

/// <summary>
/// High-level world pathfinding service.
/// Mirrors Bubble.D3.Bot's WorldPathFinderService: finds paths across the world graph.
/// </summary>
public sealed class WorldPathFinderService
{
    private readonly AStarService _aStar = new();
    private WorldGraphData? _graphData;

    public void LoadWorldGraph(WorldGraphData data)
    {
        _graphData = data;
        Log.Information("World graph loaded: {Vertices} vertices, {Edges} edges",
            data.VertexCount, data.EdgeCount);
    }

    public List<WorldPathStep>? FindPath(long fromMapId, int fromZone, long toMapId, int toZone)
    {
        if (_graphData is null)
        {
            Log.Warning("World graph not loaded");
            return null;
        }

        var start = new WorldPosition { MapId = fromMapId, ZoneId = fromZone };
        var goal = new WorldPosition { MapId = toMapId, ZoneId = toZone };

        return _aStar.FindPath(start, goal, _graphData);
    }
}

public sealed class WorldPosition
{
    public long MapId { get; set; }
    public int ZoneId { get; set; }
}

/// <summary>
/// In-memory world graph data structure for A* pathfinding.
/// </summary>
public sealed class WorldGraphData
{
    private readonly List<WorldVertexData> _vertices = [];
    private readonly Dictionary<int, List<WorldEdgeData>> _adjacency = [];

    public int VertexCount => _vertices.Count;
    public int EdgeCount => _adjacency.Values.Sum(e => e.Count);

    public int AddVertex(long mapId, int zoneId)
    {
        int id = _vertices.Count;
        _vertices.Add(new WorldVertexData { Id = id, MapId = mapId, ZoneId = zoneId });
        return id;
    }

    public void AddEdge(int fromVertexId, int toVertexId, double weight = 1.0)
    {
        if (!_adjacency.TryGetValue(fromVertexId, out var edges))
        {
            edges = [];
            _adjacency[fromVertexId] = edges;
        }
        edges.Add(new WorldEdgeData { ToVertexId = toVertexId, Weight = weight });
    }

    public int FindVertex(long mapId, int zoneId) =>
        _vertices.FindIndex(v => v.MapId == mapId && v.ZoneId == zoneId);

    public WorldVertexData GetVertex(int id) => _vertices[id];
    public List<WorldEdgeData> GetEdges(int vertexId) => _adjacency.GetValueOrDefault(vertexId) ?? [];

    public double EstimateDistance(int fromVertexId, int toVertexId)
    {
        // Simple heuristic: could be improved with map coordinates
        return 1.0;
    }
}

public sealed class WorldVertexData
{
    public int Id { get; set; }
    public long MapId { get; set; }
    public int ZoneId { get; set; }
}

public sealed class WorldEdgeData
{
    public int ToVertexId { get; set; }
    public double Weight { get; set; } = 1.0;
}
