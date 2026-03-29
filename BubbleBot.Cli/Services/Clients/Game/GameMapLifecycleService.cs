using BubbleBot.Cli.Repository;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Maps;
using BubbleBot.Cli.Services.TreasureHunts;
using BubbleBot.Cli.Services.TreasureHunts.Models;

namespace BubbleBot.Cli.Services.Clients.Game;

internal sealed class GameMapLifecycleService : GameClientServiceBase
{
    private readonly GameTravelService _travelService;
    private readonly GameMonsterDiscoveryService _monsterDiscoveryService;

    public GameMapLifecycleService(BotGameClientContext       context,
                                   ClientTransportService     transportService,
                                   GameNotificationService    notificationService,
                                   GameTravelService          travelService,
                                   GameMonsterDiscoveryService monsterDiscoveryService)
        : base(context, transportService, notificationService)
    {
        _travelService = travelService;
        _monsterDiscoveryService = monsterDiscoveryService;
    }

    public Map? CreateMap(long                            mapId,
                          List<InteractiveElement>        elements,
                          List<ActorPositionInformation>  actors,
                          bool                            isHavenBag)
    {
        var mapData = MapRepository.Instance.GetMap(mapId);
        if (mapData == null)
        {
            LogError("Map {MapId} not found", mapId);
            return null;
        }

        return new Map(mapData, elements, actors, isHavenBag, Client, _travelService);
    }

    public void OnNewMap(MapComplementaryInformationEvent mapEvent)
    {
        OccupiedStuckCounter = 0;
        LastMapChange = DateTime.UtcNow;
        LogInfo("Arrivée sur la carte {MapId}", mapEvent.MapId);
        BotManager.Instance.Clients[BotId] = _client;

        MapCurrentEvent = mapEvent;
        var map = CreateMap(mapEvent.MapId,
                            mapEvent.InteractiveElements,
                            mapEvent.Actors,
                            mapEvent.HavenBagInformation != null);

        if (map == null)
        {
            return;
        }

        Map = map;
        LogDiscord($"Arrivée sur la carte {mapEvent.MapId}, [{map.Data.PosX}, {map.Data.PosY}]");

        map.OnMapEntered(mapEvent);
        _monsterDiscoveryService.ScanMapActors(map, mapEvent.Actors);
        TreasureHuntData.OnMapChanged();

        if (mapEvent.Fights.Count > 0 && Party != null)
        {
            foreach (var fight in mapEvent.Fights)
            {
                if (fight.TeamsInformations.Any(x => x.LeaderId == Party.Leader))
                {
                    SendRequest(new FightJoinRequest
                                {
                                    FightId = fight.FightId,
                                    FighterId = Party.Leader
                                },
                                FightJoinRequest.TypeUrl);
                }
            }
        }

        if (mapEvent.HavenBagInformation != null)
        {
            AutoPath = [];
            AutoPathIndex = 0;
            AutoPathEndMapId = -1;

            HavenBag.OnEnterHavenBag(mapEvent);

            if (TreasureHuntData.CurrentCheckpoint + 1 == TreasureHuntData.TotalCheckpoint &&
                TreasureHuntData.State == TreasureHuntState.HuntActive)
            {
                TreasureHuntData.GiveUp(GiveUpReason.FightLost);
                HavenBag.EnterHavenBag(HavenBagEnterReason.GoToFirstStep);
            }
        }
        else if (AutoPath.Count > 0)
        {
            AutoPathIndex++;
            Client.StartAutoPath();
        }

        Client.DoWork();
    }
}
