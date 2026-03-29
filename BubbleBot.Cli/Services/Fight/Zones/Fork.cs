using Bubble.DamageCalculation;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Fight.Zones;


public class Fork : DisplayZone
{
    public uint Length { get; }

    public Fork(uint size, Map map) : base(SpellShape.F, 0, size, map)
    {
        Length = size + 1;
    }

    public override uint Surface => 1 + 3 * Length;

    public override IEnumerable<Cell> GetCells(uint cellId = 0)
    {
        var origin = MapPoint.GetPoint(cellId)!;
        var cells  = new List<Cell>() { Map.Data.GetCell(cellId)!, };

        var sign     = Orientation is Direction.NorthWest or Direction.SouthWest ? -1 : 1;
        var axisFlag = Orientation is Direction.NorthWest or Direction.SouthEast;

        for (var i = 1; i <= Length; i++)
        {
            for (var j = -1; j <= 1; j++)
            {
                int x;
                int y;

                if (axisFlag)
                {
                    x = origin.X + i * sign;
                    y = origin.Y + j * i;
                }
                else
                {
                    x = origin.X + j * i;
                    y = origin.Y + i * sign;
                }

                TryAddCell(x, y, cells);
            }
        }

        return cells;
    }
}