using OtomAI.Bot.Repository.Maps;
using OtomAI.Datacenter.Models;

namespace OtomAI.Bot.Maps;

/// <summary>
/// In-map A* pathfinding service. Wraps the existing Pathfinder.
/// Mirrors Bubble.D3.Bot's PathFindingClientService: handles obstacles,
/// entity avoidance, fight pathfinding.
/// </summary>
public sealed class PathfindingClientService
{
    public List<int>? FindPath(MapData map, int start, int goal, HashSet<int>? blockedCells = null)
    {
        return Navigation.Pathfinder.FindPath(map, start, goal, blockedCells);
    }

    public int GetDistance(int cellA, int cellB) =>
        MapPoint.DistanceBetween(cellA, cellB);

    public List<int> GetReachableCells(MapData map, int startCellId, int maxRange, HashSet<int>? blocked = null)
    {
        var reachable = new List<int>();
        var cells = map.Cells.ToDictionary(c => c.Id);

        for (int cellId = 0; cellId < MapPoint.TotalCells; cellId++)
        {
            if (!cells.TryGetValue(cellId, out var cell) || !cell.Walkable) continue;
            if (blocked?.Contains(cellId) == true) continue;
            if (GetDistance(startCellId, cellId) <= maxRange)
                reachable.Add(cellId);
        }

        return reachable;
    }
}
