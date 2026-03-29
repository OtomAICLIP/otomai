using Bubble.Core.Datacenter.Datacenter.WorldGraph;
using Bubble.Core.Services;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Maps.World;

public class WorldPathFinderService : Singleton<WorldPathFinderService>
{
    private WorldGraphEntry _worldGraph = null!;
    private Action<List<WorldGraphEdge>>? _callback;
    private WorldGraphVertex? _from;
    private long _to;
    private int _linkedZone;
    
    public void Initialize()
    {
        _worldGraph = MapRepository.Instance.GetWorldGraph();
    }
    
    public WorldGraphEntry GetWorldGraph()
    {
        return _worldGraph;
    }
    
    public void FindPath(long fromMapId, 
                         int linkedZoneRp,
                         long destinationMapId,
                         Action<List<WorldGraphEdge>> callback)
    {
        _from = _worldGraph.GetVertex(fromMapId, linkedZoneRp);
        
        if(_from == null)
        {
            callback([]);
            return;
        }

        _linkedZone = 1;
        _callback   = callback;
        _to         = destinationMapId;
        
        Next();
    }

    public void OnAStarComplete(List<WorldGraphEdge>? path)
    {
        if(path == null || path.Count == 0)
        {
            Next();
            return;
        }
        
        _callback?.Invoke(path);
    }
    
    public void Next()
    {
        var dest = _worldGraph.GetVertex(_to, _linkedZone++);
        
        if (dest == null)
        {
            _callback?.Invoke([]);
            return;
        }

        var astar = new AStarService();
        astar.Initialize();
        
        astar.Search(_from!, dest, OnAStarComplete);
    } 
}