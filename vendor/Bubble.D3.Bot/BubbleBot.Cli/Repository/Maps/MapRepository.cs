using System.Diagnostics;
using System.Text.Json;
using Bubble.Core.Datacenter;
using Bubble.Core.Datacenter.Datacenter.Monster;
using Bubble.Core.Datacenter.Datacenter.World;
using Bubble.Core.Datacenter.Datacenter.WorldGraph;
using Bubble.Core.Services;
using Serilog;

namespace BubbleBot.Cli.Repository.Maps;

public class MapRepository : Singleton<MapRepository>
{
    private Dictionary<long, MapPositions> _mapPositions = new();
    private Dictionary<int, SubAreas> _subAreas = new();
    private Dictionary<ushort, Monsters> _monsters = new();
    private Dictionary<long, MapRecord> _mapDatas = new();
    private readonly Dictionary<long, MapData> _maps = new();
    private Dictionary<int, Waypoints> _waypoints = new();
    
    private WorldGraphEntry _worldGraph = new()
    {
        Vertices = new Dictionary<long, Dictionary<int, WorldGraphVertex>>(),
        Edges = new Dictionary<long, Dictionary<long, WorldGraphEdge>>(),
        OutGoingEdges = new Dictionary<long, List<WorldGraphEdge>>()
    };

    public void Initialize()
    {
        _worldGraph = DatacenterService.LoadWorldGraph() ?? throw new Exception("Failed to load world graph");
        
        _mapPositions = (DatacenterService.Load<MapPositions>()).Values.ToDictionary(x => (long)x.Id);
        _subAreas = (DatacenterService.Load<SubAreas>()).Values.ToDictionary(x => x.Id);
        _monsters = (DatacenterService.Load<Monsters>()).Values.ToDictionary(x => x.Id);
        
        _waypoints = (DatacenterService.Load<Waypoints>()).Values.ToDictionary(x => x.Id);
        

        var scrollActions = DatacenterService.Load<MapScrollActions>();
        var sw = Stopwatch.StartNew();
        var mapsDatas = File.ReadAllText("Data/maps.json");

        _mapDatas = (JsonSerializer.Deserialize<List<MapRecord>>(mapsDatas))!
            .ToDictionary(x => x.Id);

        sw.Stop();

        foreach (var map in _mapDatas.Values)
        {
            var mapPos = _mapPositions.GetValueOrDefault(map.Id);
            if (mapPos == null)
            {
                continue;
            }
            
            var scrollAction = scrollActions.GetValueOrDefault(map.Id);
            
            _maps[map.Id] = new MapData(mapPos,
                                        map.GetCells(),
                                        map.GetInteractiveElements(),
                                        map.BottomNeighbourId,  
                                        map.LeftNeighbourId,
                                        map.RightNeighbourId,
                                        map.TopNeighbourId,
                                        scrollAction?.BottomMapId ?? map.BottomNeighbourId,
                                        scrollAction?.LeftMapId ?? map.LeftNeighbourId,
                                        scrollAction?.RightMapId ?? map.RightNeighbourId,
                                        scrollAction?.TopMapId ?? map.TopNeighbourId);
        }
    }

    public Monsters? GetMonster(ushort id)
    {
        return _monsters.GetValueOrDefault(id);
    }

    public MapData? GetMap(long mapId)
    {
        return _maps.TryGetValue(mapId, out var map) ? map : null;
    }

    public MapRecord? GetMapData(long mapId)
    {
        return _mapDatas.TryGetValue(mapId, out var map) ? map : null;
    }

    public List<Waypoints> GetWayPoints()
    {
        return _waypoints.Where(x => x.Value.Activated)
                         .Select(x => x.Value)
                         .ToList();
    }

    public SubAreas? GetSubArea(int subAreaId)
    {
        return _subAreas.TryGetValue(subAreaId, out var subArea) ? subArea : null;
    }

    public MapData? GetMap(int x, int y, int fromSubArea, int fromArea)
    {
        var maps = _maps.Values.Where(map => map.PosX == x && map.PosY == y && map.WorldMap == 1).ToList();

        var mapScores = new Dictionary<MapData, int>();

        foreach (var map in maps)
        {
            var subArea = GetSubArea(map.SubAreaId);
            if (subArea == null)
            {
                continue;
            }

            var score = 0;
            if (subArea.Neighbors.Contains(fromSubArea))
            {
                score += 50;
            }

            if (map.SubAreaId == fromSubArea)
            {
                score += 100;
            }

            if (subArea.AreaId == fromArea)
            {
                score += 10;
            }

            if (map.HasPriorityOnWorldmap)
            {
                score += 100;
            }

            if (map.Outdoor)
            {
                score += 10;
            }

            mapScores[map] = score;
        }

        // return the map with the highest score
        return mapScores.OrderByDescending(z => z.Value).FirstOrDefault().Key;
    }

    public WorldGraphEntry GetWorldGraph()
    {
        return _worldGraph;
    }

    public MapPositions? GetMapPosition(long mapId)
    {
        return _mapPositions.GetValueOrDefault(mapId);
    }

}