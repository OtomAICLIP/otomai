using OtomAI.Bot.Repository.Maps;
using OtomAI.Datacenter.Models;

namespace OtomAI.Bot.Fight;

/// <summary>
/// Line-of-sight detection for spell casting.
/// Mirrors Bubble.D3.Bot's FightLosDetectorService.
/// </summary>
public static class FightLosDetectorService
{
    public static bool HasLineOfSight(MapData map, int fromCellId, int toCellId)
    {
        if (fromCellId == toCellId) return true;

        var from = new MapPoint(fromCellId);
        var to = new MapPoint(toCellId);

        // Bresenham line algorithm
        int dx = Math.Abs(to.X - from.X);
        int dy = Math.Abs(to.Y - from.Y);
        int sx = from.X < to.X ? 1 : -1;
        int sy = from.Y < to.Y ? 1 : -1;
        int err = dx - dy;
        int x = from.X, y = from.Y;

        var cells = map.Cells.ToDictionary(c => c.Id);

        while (x != to.X || y != to.Y)
        {
            int cellId = y * MapPoint.MapWidth + x;
            if (cellId != fromCellId && cells.TryGetValue(cellId, out var cell) && !cell.LineOfSight)
                return false;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 < dx) { err += dx; y += sy; }
        }

        return true;
    }
}
