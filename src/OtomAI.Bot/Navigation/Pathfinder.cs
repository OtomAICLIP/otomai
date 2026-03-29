using OtomAI.Datacenter.Models;

namespace OtomAI.Bot.Navigation;

/// <summary>
/// A* pathfinder on the cell grid (map-level) and world graph (inter-map).
/// Based on Bubble.D3.Bot's implementation:
/// - 8-directional movement (cardinal + diagonal)
/// - 500 node search limit
/// - Manhattan distance heuristic
/// - +20 cost for entity-blocked cells
/// </summary>
public static class Pathfinder
{
    private const int MaxSearchNodes = 500;
    private const int EntityBlockedCost = 20;

    // 8-direction offsets for the Dofus cell grid
    private static readonly (int dx, int dy)[] Directions =
    [
        (1, 0), (-1, 0), (0, 1), (0, -1),
        (1, 1), (1, -1), (-1, 1), (-1, -1),
    ];

    /// <summary>
    /// Find shortest path between cells on a single map.
    /// Returns list of cell IDs from start (exclusive) to goal (inclusive).
    /// </summary>
    public static List<int>? FindPath(MapData map, int startCellId, int goalCellId, HashSet<int>? blockedCells = null)
    {
        if (startCellId == goalCellId) return [];

        var cells = map.Cells.ToDictionary(c => c.Id);
        if (!cells.ContainsKey(startCellId) || !cells.ContainsKey(goalCellId)) return null;

        var openSet = new PriorityQueue<int, int>();
        var cameFrom = new Dictionary<int, int>();
        var gScore = new Dictionary<int, int> { [startCellId] = 0 };
        openSet.Enqueue(startCellId, 0);

        int nodesExplored = 0;
        while (openSet.Count > 0 && nodesExplored < MaxSearchNodes)
        {
            int current = openSet.Dequeue();
            nodesExplored++;

            if (current == goalCellId)
                return ReconstructPath(cameFrom, current);

            foreach (int neighbor in GetNeighbors(current, cells, blockedCells))
            {
                int moveCost = 1;
                if (blockedCells?.Contains(neighbor) == true)
                    moveCost += EntityBlockedCost;

                int tentativeG = gScore[current] + moveCost;
                if (tentativeG < gScore.GetValueOrDefault(neighbor, int.MaxValue))
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    int fScore = tentativeG + ManhattanDistance(neighbor, goalCellId);
                    openSet.Enqueue(neighbor, fScore);
                }
            }
        }

        return null; // No path found
    }

    private static IEnumerable<int> GetNeighbors(int cellId, Dictionary<int, Cell> cells, HashSet<int>? blocked)
    {
        // Dofus grid uses a staggered layout. Cell coordinates:
        // x = cellId % 14, y = cellId / 14 (for a 14-wide grid within 560 cells)
        int x = cellId % 14;
        int y = cellId / 14;

        foreach (var (dx, dy) in Directions)
        {
            int nx = x + dx;
            int ny = y + dy;
            int neighborId = ny * 14 + nx;

            if (cells.TryGetValue(neighborId, out var cell) && cell.Walkable)
                yield return neighborId;
        }
    }

    private static int ManhattanDistance(int a, int b)
    {
        int ax = a % 14, ay = a / 14;
        int bx = b % 14, by = b / 14;
        return Math.Abs(ax - bx) + Math.Abs(ay - by);
    }

    private static List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
    {
        var path = new List<int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        path.RemoveAt(0); // Remove start cell
        return path;
    }
}
