using Bubble.DamageCalculation;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Fight.Zones;

public class Lozenge : DisplayZone
{
    public uint Radius { get; }
    public uint MinRadius { get; }

    public Lozenge(SpellShape shape, uint otherSize, uint size, Map map) : base(shape, otherSize, size, map)
    {
        MinRadius = otherSize;
        Radius    = size;
    }

    public override uint Surface => (uint)(Math.Pow(Radius + 1, 2) + Math.Pow(Radius, 2));

    public override IEnumerable<Cell> GetCells(uint cellId = 0)
    {
        var cells  = new List<Cell>();
        var origin = MapPoint.GetPoint(cellId)!;
        var x      = origin.X;
        var y      = origin.Y;

        if (Radius == 0)
        {
            if (MinRadius == 0)
            {
                cells.Add(Map.Data.GetCell(cellId)!);
            }

            return cells;
        }

        for (var radiusStep = (int)Radius; radiusStep >= MinRadius; radiusStep--)
        {
            for (var i = -radiusStep; i <= radiusStep; i++)
            {
                for (var j = -radiusStep; j <= radiusStep; j++)
                {
                    if (Math.Abs(i) + Math.Abs(j) != radiusStep)
                    {
                        continue;
                    }

                    var xResult = x + i;
                    var yResult = y + j;
                    TryAddCell(xResult, yResult, cells);
                }
            }
        }

        return cells;
    }
}