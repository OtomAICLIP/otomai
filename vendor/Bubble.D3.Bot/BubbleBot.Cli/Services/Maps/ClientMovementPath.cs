using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Maps;


public class ClientMovementPath
{
    private const int MaxPathLength = 100;

    public MapPoint Start { get; set; }
    public MapPoint End { get; set; }
    public List<PathElement> Path { get; set; }


    public ClientMovementPath()
    {
        Start = MapPoint.GetPoint(0)!;
        End   = MapPoint.GetPoint(0)!;
        Path  = new List<PathElement>();
    }

    public void AddPoint(PathElement element)
    {
        Path.Add(element);
    }

    public void Compress()
    {
        if(Path.Count <= 1)
            return;

        var elem = Path.Count - 1;

        while (elem > 0)
        {
            if(Path[elem].Orientation == Path[elem - 1].Orientation)
            {
                Path.RemoveAt(elem);
            }
            
            elem--;
        }
    }
    public IEnumerable<int> GetServerPath()
    {
        Compress();

        var movement        = new List<int>();
        var lastOrientation = Path[0].Orientation;
        foreach (var path in Path)
        {
            lastOrientation = path.Orientation;
            var value = ((int)lastOrientation & 0x07) << 12 | path.Step.CellId & 0xFFF;
            var keyMovement = (uint)(path.Step.CellId & 0x3FF) | ((lastOrientation & 0x7) << 12);

            movement.Add((short)keyMovement);
        }

        var lastCell  = End.CellId;
        var lastValue = ((int)lastOrientation & 0x07) << 12 | lastCell & 0xFFF;
        var keyMovecment = (uint)(lastCell & 0x3FF) | ((lastOrientation & 0x7) << 12);

        movement.Add((short)keyMovecment);

        return movement;
    }
    
    public IEnumerable<int> GetClientPath()
    {
        return Path.Select(path => path.Step.CellId);
    }

    public ClientMovementPath Clone()
    {
        var path = new ClientMovementPath
        {
            Start = Start,
            End   = End
        };

        foreach (var pathElement in Path)
        {
            path.AddPoint(pathElement);
        }

        return path;
    }
}

public class PathElement
{
    public MapPoint Step { get; set; }
    public uint Orientation { get; set; }

    public PathElement(MapPoint step, uint orientation)
    {
        Step        = step;
        Orientation = orientation;
    }
}