using Bubble.DamageCalculation;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Fight.Zones;


public class ZRectangle : DisplayZone
{
    protected readonly uint Width;
    protected readonly uint Height;
    public uint MinRadius { get; }
    public bool DiagonalFree { get; }

    public ZRectangle(uint minRadius, uint size, bool isDiagonalFree, Map map) : base(SpellShape.Unknown,
        minRadius,
        size,
        map)
    {
        DiagonalFree = isDiagonalFree;
        Width = minRadius;
        Height = size > 0 ? size : Width;
        MinRadius = minRadius;
    }

    public override uint Surface => (uint)Math.Pow(Width + Height + 1, 2);

    public override IEnumerable<Cell> GetCells(uint cellId = 0)
    {
        var cells = new List<Cell>();
        var origin = MapPoint.GetPoint(cellId)!;
        var x = origin.X;
        var y = origin.Y;

        for (var i = x - Width; i <= x + Width; i++)
        {
            for (var j = y - Height; j <= y + Height; j++)
            {
                if (MinRadius != 0 && Math.Abs(x - i) + Math.Abs(y - j) < MinRadius)
                {
                    continue;
                }

                if (!DiagonalFree || Math.Abs(x - 1) != Math.Abs(y - j))
                {
                    TryAddCell((int)i, (int)j, cells);
                }
            }
        }

        return cells;
    }
}