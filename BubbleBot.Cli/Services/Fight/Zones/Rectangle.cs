using Bubble.DamageCalculation;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Fight.Zones;


public class Rectangle : DisplayZone
{
    public uint Width { get; }
    public uint Height { get; }

    public Rectangle(uint otherSize, uint size, Map map) : base(SpellShape.R, otherSize, size, map)
    {
        if (OtherSize < 1)
        {
            OtherSize = 1;
        }

        if (Size < 1)
        {
            Size = 1;
        }

        Width  = 1 + Size * 2;
        Height = 1 + OtherSize;
    }

    public override uint Surface => Width * Height;

    public override IEnumerable<Cell> GetCells(uint cellId = 0)
    {
        var origin   = MapPoint.GetPoint(cellId)!;
        var cells    = new List<Cell>();
        var sign     = Orientation is Direction.NorthWest or Direction.SouthWest ? -1 : 1;
        var axisFlag = Orientation is Direction.NorthEast or Direction.SouthWest;

        for (var i = 0; i < Height; i++)
        {
            for (var j = 0; j < Width; j++)
            {
                var x = 0;
                var y = 0;

                if (axisFlag)
                {
                    x = origin.X + j - (int)Math.Floor(Width / 2f);
                    y = origin.Y + i * sign;
                }
                else
                {
                    x = origin.X + i * sign;
                    y = origin.Y + j - (int)Math.Floor(Width / 2f);
                }

                TryAddCell(x, y, cells);
            }
        }

        return cells;
    }
}