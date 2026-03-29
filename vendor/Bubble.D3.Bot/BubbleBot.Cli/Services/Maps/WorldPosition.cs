using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Maps;

public class WorldPosition
{
    private Cell _cell;

    public WorldPosition(MapData map, Cell cell, Direction orientation)
    {
        _cell       = cell;
        Map         = map;
        Orientation = orientation;

        MapPoint = MapPoint.GetPoint(cell.Id)!;
    }

    public WorldPosition(MapObjectPosition position)
    {
        Map         = MapRepository.Instance.GetMap(position.MapId)!;
        _cell       = Map.GetCell(position.CellId)!;
        Orientation = (Direction)position.Orientation;

        MapPoint = MapPoint.GetPoint(_cell.Id)!;
    }

    public Cell Cell
    {
        get => _cell;
        set
        {
            _cell    = value;
            MapPoint = MapPoint.GetPoint(value)!;
        }
    }

    public MapData Map { get; }
    public Direction Orientation { get; set; }

    public MapPoint MapPoint { get; private set; }

    public MapObjectPosition ToCharacterPosition() => new((uint)Map.Id, (short)Cell.Id, (byte)Orientation);

    public WorldPosition Clone() => new(Map, Cell, Orientation);
}