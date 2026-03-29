using OtomAI.Datacenter.Models;

namespace OtomAI.Bot.Repository.Maps;

/// <summary>
/// Map utility functions. Mirrors Bubble.D3.Bot's MapTools.
/// </summary>
public static class MapTools
{
    public static bool IsChangeMapCell(int cellId, MapData map, int direction)
    {
        var point = new MapPoint(cellId);

        return direction switch
        {
            0 => point.Y == 0,                          // Top
            2 => point.X == MapPoint.MapWidth - 1,      // Right
            4 => point.Y == MapPoint.MapHeight - 1,     // Bottom
            6 => point.X == 0,                          // Left
            _ => false
        };
    }

    public static List<int> GetEdgeCells(int direction)
    {
        var cells = new List<int>();
        for (int i = 0; i < MapPoint.TotalCells; i++)
        {
            var point = new MapPoint(i);
            bool isEdge = direction switch
            {
                0 => point.Y == 0,
                2 => point.X == MapPoint.MapWidth - 1,
                4 => point.Y == MapPoint.MapHeight - 1,
                6 => point.X == 0,
                _ => false
            };
            if (isEdge) cells.Add(i);
        }
        return cells;
    }
}
