using Bubble.DamageCalculation;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Fight.Zones;

public class Line : DisplayZone
{
    public uint Radius { get; }
    public uint MinRadius { get; }
    public bool IsFromCaster { get; }

    private readonly bool _stopAtTarget;
    private readonly uint _casterCellId;

    public Line(SpellShape shape, uint otherSize, uint size, Map map,
                bool fromCaster = false, bool stopAtTarget = false, uint casterCellId = 0)
        : base(shape, otherSize, size, map)
    {
        Radius        = size;
        MinRadius     = otherSize;
        IsFromCaster  = fromCaster;
        _stopAtTarget = stopAtTarget;
        _casterCellId = casterCellId;
    }

    public override uint Surface => Radius + 1;

    public override IEnumerable<Cell> GetCells(uint cellId = 0)
    {
        var cells  = new List<Cell>();
        var origin = !IsFromCaster ? MapPoint.GetPoint(cellId)! : MapPoint.GetPoint(_casterCellId)!;
        var x      = origin.X;
        var y      = origin.Y;
        var length = !IsFromCaster ? Radius : Radius + MinRadius - 1;

        if (IsFromCaster && _stopAtTarget)
        {
            var distance = (uint)origin.ManhattanDistanceTo(MapPoint.GetPoint(cellId)!);
            length = distance < length ? distance : length;
        }

        for (var r = (int)MinRadius; r <= length; r++)
        {
            switch (Orientation)
            {
                case Direction.West:
                    TryAddCell(x - r, y - r, cells);
                    break;
                case Direction.North:
                    TryAddCell(x - r, y + r, cells);
                    break;
                case Direction.East:
                    TryAddCell(x + r, y + r, cells);
                    break;
                case Direction.South:
                    TryAddCell(x + r, y - r, cells);
                    break;
                case Direction.NorthWest:
                    TryAddCell(x - r, y, cells);
                    break;
                case Direction.SouthWest:
                    TryAddCell(x, y - r, cells);
                    break;
                case Direction.SouthEast:
                    TryAddCell(x + r, y, cells);
                    break;
                case Direction.NorthEast:
                    TryAddCell(x, y + r, cells);
                    break;
            }
        }

        return cells;
    }
}