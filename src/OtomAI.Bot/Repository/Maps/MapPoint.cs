namespace OtomAI.Bot.Repository.Maps;

/// <summary>
/// Coordinate system for the Dofus cell grid.
/// Mirrors Bubble.D3.Bot's MapPoint: cell ID ↔ (x, y) conversion.
/// Dofus uses a 14-wide diamond grid with 560 cells per map.
/// </summary>
public readonly struct MapPoint
{
    public const int MapWidth = 14;
    public const int MapHeight = 20;
    public const int TotalCells = 560;

    public int CellId { get; }
    public int X { get; }
    public int Y { get; }

    public MapPoint(int cellId)
    {
        CellId = cellId;
        X = cellId % MapWidth;
        Y = cellId / MapWidth;
    }

    public MapPoint(int x, int y)
    {
        X = x;
        Y = y;
        CellId = y * MapWidth + x;
    }

    public static int DistanceBetween(MapPoint a, MapPoint b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    public static int DistanceBetween(int cellA, int cellB) =>
        DistanceBetween(new MapPoint(cellA), new MapPoint(cellB));

    public static bool IsValidCell(int cellId) =>
        cellId >= 0 && cellId < TotalCells;

    public override string ToString() => $"({X},{Y})#{CellId}";
}
