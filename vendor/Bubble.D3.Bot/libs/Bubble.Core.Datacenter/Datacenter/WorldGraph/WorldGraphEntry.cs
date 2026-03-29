using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.WorldGraph;

public sealed class WorldGraphEntry
{
    public required Dictionary<long, Dictionary<int, WorldGraphVertex>> Vertices { get; set; }
    public required Dictionary<long, Dictionary<long, WorldGraphEdge>> Edges { get; set; }
    public required Dictionary<long, List<WorldGraphEdge>> OutGoingEdges { get; set; }

    public WorldGraphVertex? GetVertex(long mapId, int mapRpZone)
    {
        if(!Vertices.TryGetValue(mapId, out var vertexes))
        {
            return null;
        }
        
        if(!vertexes.TryGetValue(mapRpZone, out var vertex))
        {
            return null;
        }
        
        return vertex;
    }

    public WorldGraphEdge? GetEdge(WorldGraphVertex from, WorldGraphVertex to)
    {
        if(!Edges.TryGetValue((long)from.Uid, out var edges))
        {
            return null;
        }
        
        if(!edges.TryGetValue((long)to.Uid, out var edge))
        {
            return null;
        }
        
        return edge;
    }

    public List<WorldGraphEdge> GetOutgoingEdgesFromVertex(WorldGraphVertex from)
    {
        if(!OutGoingEdges.TryGetValue((long)from.Uid, out var edges))
        {
            return new();
        }
        
        return edges;
    }
}