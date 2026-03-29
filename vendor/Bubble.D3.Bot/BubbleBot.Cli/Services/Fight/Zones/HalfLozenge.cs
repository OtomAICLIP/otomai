using Bubble.DamageCalculation;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Fight.Zones;


public class HalfLozenge : DisplayZone
{
    public uint Radius { get; }
    public uint MinRadius { get; }

    public HalfLozenge(uint otherSize, uint size, Map map) : base(SpellShape.U, otherSize, size, map)
    {
        MinRadius = otherSize;
        Radius    = size;
    }

    public override uint Surface => Radius * 2 + 1;

    public override IEnumerable<Cell> GetCells(uint cellId = 0)
    {
        var cells  = new List<Cell>();
        var origin = MapPoint.GetPoint(cellId)!;
        var x      = origin.X;
        var y      = origin.Y;

        if (MinRadius == 0)
        {
            cells.Add(Map.Data.GetCell(cellId)!);
        }

        for (var i = 1; i <= Radius; i++)
        {
            switch (Orientation)
            {
                case Direction.NorthWest:
                    TryAddCell(x + i, y + i, cells);
                    TryAddCell(x + i, y - i, cells);
                    break;
                case Direction.NorthEast:
                    TryAddCell(x - i, y - i, cells);
                    TryAddCell(x + i, y - i, cells);
                    break;
                case Direction.SouthEast:
                    TryAddCell(x - i, y + i, cells);
                    TryAddCell(x - i, y - i, cells);
                    break;
                case Direction.SouthWest:
                    TryAddCell(x - i, y + i, cells);
                    TryAddCell(x + i, y + i, cells);
                    break;
            }
        }

        return cells;
    }
}