using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Maps;

public class MovementPath
{
    public Cell[] CellsPath;
    private WorldPosition[]? _compressedPath;
    private WorldPosition? _endPathPosition;
    private MapPoint[] _path;

    public MovementPath(MapData map, IEnumerable<Cell> path)
    {
        Map       = map;
        CellsPath = path.ToArray();
        _path     = CellsPath.Select(entry => MapPoint.GetPoint(entry)!).ToArray();
    }

    private MovementPath(MapData map, IEnumerable<WorldPosition> compressedPath)
    {
        Map             = map;
        _compressedPath = compressedPath.ToArray();
        CellsPath       = BuildCompletePath();
        _path           = CellsPath.Select(entry => MapPoint.GetPoint(entry)!).ToArray();
    }

    private MapData Map { get; }
    public Cell StartCell => CellsPath[0];
    public Cell EndCell => CellsPath[^1];

    public WorldPosition EndPathPosition
    {
        get { return _endPathPosition ??= new WorldPosition(Map, EndCell, GetEndCellDirection()); }
    }

    public int MpCost => CellsPath.Length - 1;

    public Cell[] Cells
    {
        get => CellsPath;
        set
        {
            CellsPath        = value;
            _endPathPosition = null;
            _path            = CellsPath.Select(entry => new MapPoint(entry)).ToArray();
        }
    }

    public bool IsEmpty()
    {
        return CellsPath.Length == 0;
    }

    private Direction GetEndCellDirection()
    {
        if (CellsPath.Length <= 1)
        {
            return Direction.East;
        }

        return _compressedPath != null
            ? _compressedPath.Last().Orientation
            : _path[^2].OrientationToAdjacent(_path[^1]);
    }

    public WorldPosition[] GetCompressedPath()
    {
        return _compressedPath ??= BuildCompressedPath();
    }

    public IEnumerable<Cell> GetPath()
    {
        return CellsPath;
    }

    public bool Contains(short cellId)
    {
        return CellsPath.Any(entry => entry.Id == cellId);
    }

    public IEnumerable<int> GetServerPathKeys()
    {
        return CellsPath.Select(entry => entry.Id);
    }

    public void CutPath(int index, bool skip = false)
    {
        if (index >= CellsPath.Length || index < 0)
        {
            return;
        }

        CellsPath = skip ? CellsPath.Skip(index).ToArray() : CellsPath.Take(index).ToArray();

        _path = CellsPath.Select(entry => new MapPoint(entry)).ToArray();

        _endPathPosition = new WorldPosition(Map, EndCell, GetEndCellDirection());
    }
    
    public int GetFinalCellWithMp(int mp)
    {      
        var cells = CellsPath.Take(mp).ToArray();
        
        if (cells.Length == 0)
        {
            return StartCell.Id;
        }
        
        if (mp >= MpCost)
        {
            return EndCell.Id;
        }

        return cells.Last().Id;
    }

    private WorldPosition[] BuildCompressedPath()
    {
        switch (CellsPath.Length)
        {
            case <= 0:
                return Array.Empty<WorldPosition>();
            case <= 1:
                return new[] { new WorldPosition(Map, CellsPath[0], Direction.East), };
        }

        // build the path
        var path = new List<WorldPosition>();
        for (var i = 1; i < CellsPath.Length; i++)
        {
            path.Add(new WorldPosition(Map, CellsPath[i - 1], _path[i - 1].OrientationToAdjacent(_path[i])));
        }

        path.Add(new WorldPosition(Map, CellsPath[^1], path[^1].Orientation));

        // compress it
        if (path.Count <= 0) return path.ToArray();

        var i2 = path.Count - 2; // we don't touch to the last vector
        while (i2 > 0)
        {
            if (path[i2].Orientation == path[i2 - 1].Orientation) path.RemoveAt(i2);

            i2--;
        }

        return path.ToArray();
    }

    public WorldPosition[] BuildCompletePositions()
    {
        if (_compressedPath == null) return [];
        
        switch (CellsPath.Length)
        {
            case <= 0:
                return [];
            case <= 1:
                return [new WorldPosition(Map, CellsPath[0], Direction.East)];
        }

        var path = new List<WorldPosition>();
        for (var i = 1; i < CellsPath.Length; i++)
        {
            path.Add(new WorldPosition(Map, CellsPath[i - 1],
                                       _path[i - 1].OrientationToAdjacent(_path[i])));
        }

        return path.ToArray();
    }

    private Cell[] BuildCompletePath()
    {
        var completePath = new List<Cell>();

        if (_compressedPath == null) return [];
        
        for (var i = 0; i < _compressedPath.Length - 1; i++)
        {
            completePath.Add(_compressedPath[i].Cell);

            var l = 0;
            var nextPoint = _compressedPath[i].MapPoint;
            while ((nextPoint = nextPoint.GetNearestCellInDirection(_compressedPath[i].Orientation)) != null &&
                   nextPoint.CellId != _compressedPath[i + 1].Cell.Id)
            {
                if (l > MapConstants.Height * 2 + MapConstants.Width)
                {
                    throw new Exception("MovementPath too long. Maybe an orientation problem ?");
                }

                var cell = Map.Cells[nextPoint.CellId];

                if (!Map.IsCellWalkable(cell)) //Verify Hack
                    return completePath.ToArray();
                

                completePath.Add(cell);

                l++;
            }
        }

        completePath.Add(_compressedPath[^1].Cell);

        return completePath.ToArray();
    }

    public static MovementPath BuildFromCompressedPath(MapData map, IEnumerable<short> keys)
    {
        var path = from key in keys
            let cellId = key & 4095
            let direction = (Direction)((key >> 12) & 7)
            select new WorldPosition(map, map.Cells[(short)cellId], direction);

        return new MovementPath(map, path);
    }


    public static MovementPath GetEmptyPath(MapData map, Cell startCell)
    {
        return new MovementPath(map, new[] { startCell, });
    }

    public void CancelAt(Cell cell)
    {
        if (cell.Id == EndCell.Id) return;

        var index = Array.FindIndex(Cells, entry => entry.Id == cell.Id);
        if (index == -1) return;

        CutPath(index + 1);
    }
}