using Bubble.Core.Datacenter.Datacenter.WorldGraph;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Clients.Contracts;
using BubbleBot.Cli.Services.Maps.World;
using BubbleBot.Cli.Services.TreasureHunts;
using BubbleBot.Cli.Services.TreasureHunts.Models;
using Serilog;

namespace BubbleBot.Cli.Services.Clients.Game;

internal sealed class GameTravelService : GameClientServiceBase, IMapTravelService
{
    private long _worldPathId = -1;
    private long _lastWorldPathId = -1;
    private CancellationTokenSource _worldPathCts = new();

    public GameTravelService(BotGameClientContext    context,
                             ClientTransportService  transportService,
                             GameNotificationService notificationService)
        : base(context, transportService, notificationService)
    {
    }

    public void GoToMap(Map currentMap, int x, int y)
    {
        var subArea = MapRepository.Instance.GetSubArea(currentMap.Data.SubAreaId);
        var map = MapRepository.Instance.GetMap(x, y, currentMap.Data.SubAreaId, subArea?.AreaId ?? 0);

        if (map == null)
        {
            LogInfo("Map {X}, {Y} not found", x, y);
            return;
        }

        GoToMap(currentMap, map.Id);
    }

    public void GoToMapSafe(Map currentMap, long mapId)
    {
        if (Map == null)
        {
            return;
        }

        if (Map.CellId != WorldPath.WantToGoOnCellId &&
            WorldPath.WantToGoOnCellId != -1 &&
            AutoPathEndMapId == mapId)
        {
            LogInfo("On ignore le déplacement car on est déjà en mouvement vers la map {MapId}", mapId);
            return;
        }

        LogInfo("Déplacement vers la map {MapId}", mapId);
        GoToMap(currentMap, mapId);
    }

    public void GoToMap(Map currentMap, long mapId)
    {
        if (Map == null || IsInFight || currentMap.IsHavenBag || currentMap.Id == mapId)
        {
            return;
        }

        AutoPathEndMapId = mapId;
        LogInfo("Recherche d'un chemin vers la map {MapId} depuis la map {CurrentMapId}", mapId, currentMap.Id);

        var currentZoneCell = currentMap.Data.GetCell((short)currentMap.CellId);
        if (currentZoneCell == null)
        {
            LogWarning("Current zone not found on map {MapId}", currentMap.Id);
            return;
        }

        try
        {
            if (_worldPathCts.Token.CanBeCanceled)
            {
                _worldPathCts.Cancel();
            }

            _worldPathId++;
            var worldPathId = _worldPathId;

            _worldPathCts = new CancellationTokenSource();
            _worldPathCts.CancelAfter(5000);
            _worldPathCts.Token.Register(() =>
            {
                if (_lastWorldPathId == worldPathId || worldPathId != _worldPathId)
                {
                    return;
                }

                Log.Warning("World path to map {MapId} not found", mapId);
                OnWorldPathFound([]);
            });

            WorldPathFinderService.Instance.FindPath(currentMap.Id, currentZoneCell.LinkedZoneRp, mapId, OnWorldPathFound);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Error while finding path to map {MapId}", mapId);
            OnWorldPathFound([]);
        }
    }

    private void OnWorldPathFound(List<WorldGraphEdge> edges)
    {
        _lastWorldPathId = _worldPathId;
        _worldPathCts.Cancel();

        if (edges.Count == 0)
        {
            LogInfo("Unable to find path to map {MapId}", WorldPath.WantToGoOnMapId);
            TreasureHuntData.GiveUp(GiveUpReason.WorldPathNotFound);

            if (Trajet != null)
            {
                Client.DoWork(true);
            }

            return;
        }

        var lastEdge = edges[^1];
        if (lastEdge.To.MapId != AutoPathEndMapId)
        {
            LogWarning("Unable to find path to map {MapId}", WorldPath.WantToGoOnMapId);
            return;
        }

        if (lastEdge.To.MapId == 126878209)
        {
            LogInfo("Unable to find path to map {MapId}", WorldPath.WantToGoOnMapId);
            TreasureHuntData.GiveUp(GiveUpReason.MapDisallowed);
            return;
        }

        AutoPath = edges;
        AutoPathIndex = 0;
        Client.StartAutoPath();
    }
}
