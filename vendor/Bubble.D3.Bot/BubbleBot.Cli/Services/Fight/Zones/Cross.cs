using Bubble.DamageCalculation;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Fight.Zones;

public class Cross : DisplayZone
{
    private readonly uint _radius;
    private readonly uint _minRadius;
    private readonly bool _onlyPerpendicular;
    private readonly bool _diagonal;
    private readonly bool _allDirections;

    private readonly List<Direction> _disabledDirection = new();

    public Cross(SpellShape shape, uint otherSize, uint size, Map map,
                 bool diagonal = false, bool allDirections = false) : base(shape, otherSize, size, map)
    {
        _disabledDirection = new List<Direction>();
        _minRadius         = otherSize;
        _radius            = size;
        _onlyPerpendicular = shape is SpellShape.T or SpellShape.Minus;
        _diagonal          = diagonal;
        _allDirections     = allDirections;
    }

    public override uint Surface => _radius * 4 + 1;

    public override IEnumerable<Cell> GetCells(uint cellId = 0)
    {
        var cells = new List<Cell>();

        if (_minRadius == 0)
        {
            cells.Add(Map.Data.GetCell(cellId)!);
        }

        if (_onlyPerpendicular)
        {
            switch (Orientation)
            {
                case Direction.SouthEast:
                case Direction.NorthWest:
                    _disabledDirection.AddRange(new[] { Direction.SouthEast, Direction.NorthWest, });
                    break;
                case Direction.NorthEast:
                case Direction.SouthWest:
                    _disabledDirection.AddRange(new[] { Direction.NorthEast, Direction.SouthWest, });
                    break;
                case Direction.North:
                case Direction.South:
                    _disabledDirection.AddRange(new[] { Direction.North, Direction.South, });
                    break;
                case Direction.East:
                case Direction.West:
                    _disabledDirection.AddRange(new[] { Direction.East, Direction.West, });
                    break;
            }
        }

        var origin = MapPoint.GetPoint(cellId)!;

        var x = origin.X;
        var y = origin.Y;

        for (var r = (int)_radius; r > 0; r--)
        {
            if (r < _minRadius)
            {
                continue;
            }

            if (!_diagonal)
            {
                if (!_disabledDirection.Contains(Direction.SouthEast))
                {
                    TryAddCell(x + r, y, cells);
                }

                if (!_disabledDirection.Contains(Direction.NorthWest))
                {
                    TryAddCell(x - r, y, cells);
                }

                if (!_disabledDirection.Contains(Direction.NorthEast))
                {
                    TryAddCell(x, y + r, cells);
                }

                if (!_disabledDirection.Contains(Direction.SouthWest))
                {
                    TryAddCell(x, y - r, cells);
                }
            }

            if (_diagonal || _allDirections)
            {
                if (!_disabledDirection.Contains(Direction.South))
                {
                    TryAddCell(x + r, y - r, cells);
                }

                if (!_disabledDirection.Contains(Direction.North))
                {
                    TryAddCell(x - r, y + r, cells);
                }

                if (!_disabledDirection.Contains(Direction.East))
                {
                    TryAddCell(x + r, y + r, cells);
                }

                if (!_disabledDirection.Contains(Direction.West))
                {
                    TryAddCell(x - r, y - r, cells);
                }
            }
        }

        return cells;
    }
}