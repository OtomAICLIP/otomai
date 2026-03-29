using OtomAI.Bot.Repository.Maps;

namespace OtomAI.Bot.Fight.Zones;

/// <summary>
/// Spell zone shape calculation. Mirrors Bubble.D3.Bot's zone shapes.
/// Supports: Point, Cross, Line, Lozenge, Cone, Star, Square, Ring.
/// </summary>
public static class SpellZone
{
    public static List<int> GetAffectedCells(int centerCellId, int casterCellId, ZoneShape shape, int size)
    {
        return shape switch
        {
            ZoneShape.Point => [centerCellId],
            ZoneShape.Cross => GetCross(centerCellId, size),
            ZoneShape.Line => GetLine(centerCellId, casterCellId, size),
            ZoneShape.Lozenge => GetLozenge(centerCellId, size),
            ZoneShape.Square => GetSquare(centerCellId, size),
            ZoneShape.Ring => GetRing(centerCellId, size),
            _ => [centerCellId],
        };
    }

    private static List<int> GetCross(int center, int size)
    {
        var cells = new List<int> { center };
        var point = new MapPoint(center);
        for (int i = 1; i <= size; i++)
        {
            AddIfValid(cells, new MapPoint(point.X + i, point.Y));
            AddIfValid(cells, new MapPoint(point.X - i, point.Y));
            AddIfValid(cells, new MapPoint(point.X, point.Y + i));
            AddIfValid(cells, new MapPoint(point.X, point.Y - i));
        }
        return cells;
    }

    private static List<int> GetLine(int center, int caster, int size)
    {
        var cells = new List<int> { center };
        var cp = new MapPoint(center);
        var cas = new MapPoint(caster);
        int dx = Math.Sign(cp.X - cas.X);
        int dy = Math.Sign(cp.Y - cas.Y);
        for (int i = 1; i <= size; i++)
            AddIfValid(cells, new MapPoint(cp.X + dx * i, cp.Y + dy * i));
        return cells;
    }

    private static List<int> GetLozenge(int center, int size)
    {
        var cells = new List<int>();
        var point = new MapPoint(center);
        for (int dx = -size; dx <= size; dx++)
            for (int dy = -size; dy <= size; dy++)
                if (Math.Abs(dx) + Math.Abs(dy) <= size)
                    AddIfValid(cells, new MapPoint(point.X + dx, point.Y + dy));
        return cells;
    }

    private static List<int> GetSquare(int center, int size)
    {
        var cells = new List<int>();
        var point = new MapPoint(center);
        for (int dx = -size; dx <= size; dx++)
            for (int dy = -size; dy <= size; dy++)
                AddIfValid(cells, new MapPoint(point.X + dx, point.Y + dy));
        return cells;
    }

    private static List<int> GetRing(int center, int size)
    {
        var cells = new List<int>();
        var point = new MapPoint(center);
        for (int dx = -size; dx <= size; dx++)
            for (int dy = -size; dy <= size; dy++)
                if (Math.Abs(dx) + Math.Abs(dy) == size)
                    AddIfValid(cells, new MapPoint(point.X + dx, point.Y + dy));
        return cells;
    }

    private static void AddIfValid(List<int> cells, MapPoint point)
    {
        if (MapPoint.IsValidCell(point.CellId))
            cells.Add(point.CellId);
    }
}

public enum ZoneShape
{
    Point = 0,
    Cross = 1,
    Line = 2,
    Lozenge = 3,
    Cone = 4,
    Star = 5,
    Square = 6,
    Ring = 7,
}
