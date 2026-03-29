namespace OtomAI.Bot.Maps.World;

/// <summary>
/// World-level A* pathfinding between maps.
/// Mirrors Bubble.D3.Bot's AStarService: operates on WorldGraph vertices.
/// </summary>
public sealed class AStarService
{
    public List<WorldPathStep>? FindPath(
        WorldPosition start,
        WorldPosition goal,
        WorldGraphData graphData)
    {
        if (start.MapId == goal.MapId) return [];

        var openSet = new PriorityQueue<int, double>();
        var cameFrom = new Dictionary<int, int>();
        var gScore = new Dictionary<int, double>();

        // Find start vertex
        var startVertex = graphData.FindVertex(start.MapId, start.ZoneId);
        var goalVertex = graphData.FindVertex(goal.MapId, goal.ZoneId);
        if (startVertex < 0 || goalVertex < 0) return null;

        gScore[startVertex] = 0;
        openSet.Enqueue(startVertex, 0);

        while (openSet.Count > 0)
        {
            int current = openSet.Dequeue();
            if (current == goalVertex)
                return ReconstructPath(cameFrom, current, graphData);

            foreach (var edge in graphData.GetEdges(current))
            {
                double tentativeG = gScore[current] + edge.Weight;
                if (tentativeG < gScore.GetValueOrDefault(edge.ToVertexId, double.MaxValue))
                {
                    cameFrom[edge.ToVertexId] = current;
                    gScore[edge.ToVertexId] = tentativeG;
                    double h = graphData.EstimateDistance(edge.ToVertexId, goalVertex);
                    openSet.Enqueue(edge.ToVertexId, tentativeG + h);
                }
            }
        }

        return null;
    }

    private static List<WorldPathStep> ReconstructPath(
        Dictionary<int, int> cameFrom, int current, WorldGraphData graphData)
    {
        var path = new List<WorldPathStep>();
        while (cameFrom.ContainsKey(current))
        {
            var vertex = graphData.GetVertex(current);
            path.Add(new WorldPathStep { MapId = vertex.MapId, ZoneId = vertex.ZoneId });
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }
}

public sealed class WorldPathStep
{
    public long MapId { get; set; }
    public int ZoneId { get; set; }
    public TransitionInfo? Transition { get; set; }
}

public sealed class TransitionInfo
{
    public int Type { get; set; }
    public int Direction { get; set; }
    public int CellId { get; set; }
    public int SkillId { get; set; }
}
