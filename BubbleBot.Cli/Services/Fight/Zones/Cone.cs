using Bubble.DamageCalculation;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Fight.Zones;

public class Cone : DisplayZone
{
    public uint Radius { get; }
    public uint MinRadius { get; }

    public Cone(uint alternativeSize, uint size, Map map) : base(SpellShape.V, alternativeSize, size, map)
    {
        MinRadius = alternativeSize;
        Radius    = size;
    }

    public override uint Surface => (uint)Math.Pow(Radius + 1, 2);

    public override IEnumerable<Cell> GetCells(uint cellId = 0)
    {
        var cells  = new List<Cell>();
        var origin = MapPoint.GetPoint(cellId)!;

        var x = origin.X;
        var y = origin.Y;

        if (Radius == 0)
        {
            if (MinRadius == 0)
            {
                cells.Add(Map.Data.GetCell(cellId)!);
            }

            return cells;
        }

        var step = 0;

        switch (Orientation)
        {
            case Direction.NorthWest:
                for (var i = x; i >= x - Radius; i--)
                {
                    for (var j = -step; j <= step; j++)
                    {
                        if (MinRadius == 0 || Math.Abs(x - i) + Math.Abs(j) >= MinRadius)
                        {
                            TryAddCell(i, y + j, cells);
                        }
                    }

                    step++;
                }

                break;
            case Direction.SouthWest:
                for (var j = y; j >= y - Radius; j--)
                {
                    for (var i = -step; i <= step; i++)
                    {
                        if (MinRadius == 0 || Math.Abs(i) + Math.Abs(y - j) >= MinRadius)
                        {
                            TryAddCell(x + i, j, cells);
                        }
                    }

                    step++;
                }

                break;

            case Direction.SouthEast:
                for (var i = x; i <= x + Radius; i++)
                {
                    for (var j = -step; j <= step; j++)
                    {
                        if (MinRadius == 0 || Math.Abs(x - i) + Math.Abs(j) >= MinRadius)
                        {
                            TryAddCell(i, j + y, cells);
                        }
                    }

                    step++;
                }

                break;
            case Direction.NorthEast:
                for (var j = y; j <= y + Radius; j++)
                {
                    for (var i = -step; i <= step; i++)
                    {
                        if (MinRadius == 0 || Math.Abs(i) + Math.Abs(y - j) >= MinRadius)
                        {
                            TryAddCell(x + i, j, cells);
                        }
                    }

                    step++;
                }

                break;
        }

        return cells;
    }
}