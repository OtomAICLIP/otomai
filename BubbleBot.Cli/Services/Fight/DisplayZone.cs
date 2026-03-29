using Bubble.DamageCalculation;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Fight;

public class DisplayZone
{
    public uint OtherSize { get; protected set; }
    public uint Size { get; protected set; }
    public SpellShape Shape { get; protected set; }
    public Map Map { get; }

    public Direction Orientation { get; set; } = Direction.SouthEast;

    public DisplayZone(SpellShape shape, uint otherSize, uint size, Map map)
    {
        OtherSize = otherSize;
        Size      = size;
        Shape     = shape;
        Map       = map;
    }

    public virtual bool IsInfinite => Size == 63;

    public virtual uint Surface => 0;

    public virtual IEnumerable<Cell> GetCells(uint cellId = 0)
    {
        return Array.Empty<Cell>();
    }

    protected void TryAddCell(int x, int y, IList<Cell> cells)
    {
        if (!MapPoint.IsInMap(x, y) || !Map.Data.PointMov(x, y))
        {
            return;
        }

        cells.Add(Map.Data.GetCell((short)MapPoint.GetPoint(x, y)!.CellId)!);
    }
}