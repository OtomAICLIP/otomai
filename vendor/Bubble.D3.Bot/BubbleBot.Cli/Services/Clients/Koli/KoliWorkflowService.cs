using Bubble.Shared.Protocol;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Fight;

namespace BubbleBot.Cli.Services.Clients.Koli;

internal sealed class KoliWorkflowService : KoliClientServiceBase
{
    public KoliWorkflowService(BotKoliClient          owner,
                               BotKoliClientContext   context,
                               ClientTransportService transportService,
                               KoliNotificationService notificationService)
        : base(owner, context, transportService, notificationService)
    {
    }

    public void OnConnected()
    {
        LogInfo("Koli Connected to {BotId}", BotId);

        EnsureFightInfoFromGameClient();

        SendRequest(new IdentificationRequest
                    {
                        TicketKey = Token,
                        LanguageCode = "fr"
                    },
                    IdentificationRequest.TypeUrl);

        Task.Run(async () =>
        {
            while (true)
            {
                if (!Owner.IsConnected)
                {
                    LogInfo("On arrête la boucle de connexion");
                    return;
                }

                LogInfo("Envoi d'un ping");
                SendRequest(new PingRequest
                            {
                                Quiet = true
                            },
                            PingRequest.TypeUrl,
                            true);
                await Task.Delay(30000);

                SendRequest(new DateRequest(), DateRequest.TypeUrl);
                await Task.Delay(30000);
            }
        });
    }

    public void OnDisconnected()
    {
        LogInfo("Game Disconnected from {BotId}", BotId);
        Connected = false;

        if (IsInFight)
        {
            GameClient?.Disconnect();
        }
    }

    public void PlanifyDisconnect()
    {
        IsDisconnectionPlanned = true;
        Owner.Disconnect();
    }

    public void DoWork(bool noDelay = false)
    {
        if (Map == null)
        {
            return;
        }
    }

    public void UpdateCharacterInfoFrom(EntityLook look,
                                        ActorPositionInformation.ActorInformation.RolePlayActor.NamedActor namedActorValue,
                                        EntityDisposition actorDisposition)
    {
        Info.UpdateFrom(look, namedActorValue, actorDisposition);
    }

    public void ResetWorldPath()
    {
        WorldPath.Reset();
    }

    public void OnNewMap(MapComplementaryInformationEvent mapComplementaryInformationEvent)
    {
        LogInfo("Arrivée sur la carte {MapId}", mapComplementaryInformationEvent.MapId);
        BotManager.Instance.Clients[BotId] = BotController;

        MapCurrentEvent = mapComplementaryInformationEvent;

        var map = TryCreateMap(mapComplementaryInformationEvent.MapId,
                               mapComplementaryInformationEvent.InteractiveElements,
                               mapComplementaryInformationEvent.Actors,
                               mapComplementaryInformationEvent.HavenBagInformation != null);

        if (map == null)
        {
            return;
        }

        Map = map;
        Map.OnMapEntered(mapComplementaryInformationEvent);
        DoWork();
    }

    public void OnFightMapInformation(FightMapInformationEvent fightMapInformationEvent)
    {
        var map = TryCreateMap(fightMapInformationEvent.MapId, [], [], false);

        if (map == null)
        {
            return;
        }

        Map = map;

        if (FightInfo == null || FightInfo.IsFightEnded())
        {
            FightInfo = new FightInfo(Owner, map);
        }
        else
        {
            FightInfo.Map = map;
        }

        _ = SendRequestWithDelay(new FightReadyRequest
                                 {
                                     IsReady = true
                                 },
                                 FightReadyRequest.TypeUrl,
                                 3000);
    }

    public void OnCharacterForceSelection(CharacterForceSelectionEvent characterForceSelectionEvent)
    {
        CharacterId = GameClient?.PlayerId ?? characterForceSelectionEvent.CharacterId;
        Spells = GameClient?.Spells ?? Spells;
        Info = GameClient?.Info ?? Info;

        SendRequest(new CharacterSelectionRequest
                    {
                        CharacterId = characterForceSelectionEvent.CharacterId
                    },
                    CharacterSelectionRequest.TypeUrl);
        SendRequest(new CharacterForceSelectionReadyRequest(), CharacterForceSelectionReadyRequest.TypeUrl);
    }

    public void OnFightEnded(FightEndEvent fightEndEvent)
    {
        IsInFight = false;
        FightInfo?.OnFightEndEvent(fightEndEvent);
        FightInfo = null;

        LogKoli();
        GameClient?.OnFightEnd();
    }

    public void LogKoli()
    {
        NotificationService.LogKoli();
    }

    public void SetIsAgainstBot(string fighterName)
    {
        if (GameClient != null)
        {
            GameClient.AgainstBot = fighterName;
        }
    }

    public void SetWithBot(string fighterName)
    {
        if (GameClient != null)
        {
            GameClient.WithBot = fighterName;
        }
    }

    private void EnsureFightInfoFromGameClient()
    {
        if (FightInfo != null || GameClient?.LastMapCurrentEvent == null)
        {
            return;
        }

        var map = TryCreateMap(GameClient.LastMapCurrentEvent.MapId, [], [], false);

        if (map == null)
        {
            return;
        }

        Map = map;
        FightInfo = new FightInfo(Owner, map);
    }

    private Map? TryCreateMap(long mapId,
                              List<InteractiveElement> elements,
                              List<ActorPositionInformation> actors,
                              bool isHavenBag)
    {
        var mapData = MapRepository.Instance.GetMap(mapId);

        if (mapData == null)
        {
            LogError("Map {MapId} not found", mapId);
            return null;
        }

        return new Map(mapData, elements, actors, isHavenBag, Owner);
    }
}
