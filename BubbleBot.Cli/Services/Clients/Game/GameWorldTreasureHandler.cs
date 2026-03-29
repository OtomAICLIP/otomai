using Bubble.Shared.Protocol;
using BubbleBot.Cli.Repository;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.TreasureHunts;
using BubbleBot.Cli.Services.TreasureHunts.Models;
using BubbleBot.Cli.Services.Fight;

namespace BubbleBot.Cli.Services.Clients.Game;

internal sealed class GameWorldTreasureHandler : GameClientServiceBase, IGameMessageHandler
{
    private readonly GameMapLifecycleService _mapLifecycleService;
    private readonly GameTreasureHuntService _treasureHuntService;

    public GameWorldTreasureHandler(BotGameClientContext    context,
                                    ClientTransportService  transportService,
                                    GameNotificationService notificationService,
                                    GameMapLifecycleService mapLifecycleService,
                                    GameTreasureHuntService treasureHuntService)
        : base(context, transportService, notificationService)
    {
        _mapLifecycleService = mapLifecycleService;
        _treasureHuntService = treasureHuntService;
    }

    public bool TryHandle(IProtoMessage message)
    {
        switch (message)
        {
            case MapCurrentEvent mapCurrentEvent:
                HandleMapCurrent(mapCurrentEvent);
                return true;
            case MapComplementaryInformationEvent mapComplementaryInformationEvent:
                _mapLifecycleService.OnNewMap(mapComplementaryInformationEvent);
                return true;
            case TreasureHuntEvent treasureHuntEvent:
                _treasureHuntService.OnTreasureHunt(treasureHuntEvent);
                return true;
            case TreasureHuntFinishedEvent _:
                _treasureHuntService.OnTreasureHuntFinishedEvent();
                return true;
            case TreasureHuntFlagAnswerEvent treasureHuntFlagAnswerEvent:
                if (IsInTreasureHunt())
                {
                    TreasureHuntData.OnFlagAnswer(treasureHuntFlagAnswerEvent);
                }

                return true;
            case TreasureHuntDigAnswerEvent treasureHuntDigAnswerEvent:
                if (IsInTreasureHunt())
                {
                    TreasureHuntData.OnDigAnswer(treasureHuntDigAnswerEvent);
                }

                return true;
            case TreasureHuntAnswerEvent treasureHuntAnswerEvent:
                if (treasureHuntAnswerEvent.ResultValue == TreasureHuntAnswerEvent.Result.ErrorDailyLimitExceeded)
                {
                    LogMaxDaily();
                }

                return true;
            case TeleportDestinationsEvent teleportDestinationsEvent:
                TeleportDestinationData.OnDataReceived(teleportDestinationsEvent);
                return true;
            case ShortcutBarContentEvent shortcutBarContentEvent:
                HandleShortcutBar(shortcutBarContentEvent);
                return true;
            case MapMovementEvent mapMovementEvent:
                HandleMapMovement(mapMovementEvent);
                return true;
            case MapMovementRefusedEvent mapMovementRefusedEvent:
                HandleMapMovementRefused(mapMovementRefusedEvent);
                return true;
            case GameRolePlayShowActorsEvent gameRolePlayShowActorsEvent:
                if (Map == null)
                {
                    return true;
                }

                foreach (var actor in gameRolePlayShowActorsEvent.Actors)
                {
                    Map.SetActor(actor.ActorId, actor);
                }

                return true;
            case ContextRemoveElementEvent contextRemoveElementEvent:
                Map?.RemoveActor(contextRemoveElementEvent.ElementId);
                return true;
            case ContextRemoveElementsEvent contextRemoveElementsEvent:
                if (Map == null)
                {
                    return true;
                }

                foreach (var elementId in contextRemoveElementsEvent.ElementIds)
                {
                    Map.RemoveActor(elementId);
                }

                return true;
            case LeaderPositionEvent leaderPositionEvent:
                HandleLeaderPosition(leaderPositionEvent);
                return true;
            default:
                return false;
        }
    }

    private void HandleMapCurrent(MapCurrentEvent mapCurrentEvent)
    {
        LogInfo("On demande des infos sur la carte {MapId}", mapCurrentEvent.MapId);
        LastMapCurrentEvent = mapCurrentEvent;

        SendRequest(new MapInformationRequest
                    {
                        MapId = (int)mapCurrentEvent.MapId
                    },
                    MapInformationRequest.TypeUrl);

        if (NeedNextContext)
        {
            SendRequest(new ContextReadyRequest
                        {
                            MapId = mapCurrentEvent.MapId
                        },
                        ContextReadyRequest.TypeUrl);
        }

        if (Context != ContextCreationEvent.GameContext.Fight)
        {
            return;
        }

        IsInFight = true;
        var mapFight = MapRepository.Instance.GetMap(mapCurrentEvent.MapId);
        var fightMap = new Map(mapFight!, [], [], false, Client);

        if (FightInfo == null || FightInfo.IsFightEnded())
        {
            FightInfo = new FightInfo(Client, fightMap);
        }
    }

    private void HandleShortcutBar(ShortcutBarContentEvent shortcutBarContentEvent)
    {
        if (shortcutBarContentEvent.BarType != ShortcutBar.SpellShortcutBar)
        {
            return;
        }

        TreasureHuntData.SetSpellToUse(shortcutBarContentEvent.Shortcuts.FirstOrDefault()?.ShortcutSpell.SpellId ?? 0);
        TreasureHuntData.SetSpell2ToUse(
            shortcutBarContentEvent.Shortcuts.FirstOrDefault(x => x.SlotId == 1)?.ShortcutSpell.SpellId ?? 0);
    }

    private void HandleMapMovement(MapMovementEvent mapMovementEvent)
    {
        if (Map == null)
        {
            return;
        }

        if (FightInfo != null && IsInFight)
        {
            FightInfo.OnFightMovement(mapMovementEvent);
            return;
        }

        Map.OnMapMovementEvent(mapMovementEvent);
    }

    private void HandleMapMovementRefused(MapMovementRefusedEvent mapMovementRefusedEvent)
    {
        LogWarning("Déplacement refusé vers la cellule {CellX},{CellY}",
                   mapMovementRefusedEvent.CellX,
                   mapMovementRefusedEvent.CellY);

        if (!IsInFight)
        {
            Map?.OnMapMovementRefused(mapMovementRefusedEvent);
        }

        FightInfo?.OnMapMovementRefused(mapMovementRefusedEvent);

        if (Party != null && Trajet != null && Map?.CellErrorTrial > 2)
        {
            Map.GoToCell(Map.Data.GetFreeContiguousCell(Map.CellId, true));
        }
    }

    private void HandleLeaderPosition(LeaderPositionEvent leaderPositionEvent)
    {
        if (Party == null || Map == null || Party.Leader == PlayerId)
        {
            return;
        }

        var memberIndex = Party.Members.IndexOf(PlayerId);
        if (Map.Id != leaderPositionEvent.Map.MapId)
        {
            Map.GoToMap(leaderPositionEvent.Map.MapId);
            return;
        }

        Map.GoToCell(Map.Data.GetFreeContiguousCell(leaderPositionEvent.CellId, true, memberIndex));
    }

    private bool IsInTreasureHunt()
    {
        return TreasureHuntData.State == TreasureHuntState.HuntActive;
    }
}
