using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Maps;
using BubbleBot.Cli.Services.TreasureHunts;
using BubbleBot.Cli.Services.TreasureHunts.Models;

namespace BubbleBot.Cli.Services.Clients.Game;

internal sealed class GameTreasureHuntService : GameClientServiceBase
{
    public GameTreasureHuntService(BotGameClientContext    context,
                                   ClientTransportService  transportService,
                                   GameNotificationService notificationService)
        : base(context, transportService, notificationService)
    {
    }

    public void OnTreasureHunt(TreasureHuntEvent treasureHuntEvent)
    {
        LogDiscord($"Chasse mis à jour {treasureHuntEvent.CurrentCheckPoint}/{treasureHuntEvent.TotalCheckPoint}",
                   true);
        TreasureHuntData.OnDataReceived(treasureHuntEvent);
    }

    public void OnTreasureHuntFinishedEvent()
    {
        LogInfo("Chasse terminée");

        TreasureHuntData.State = TreasureHuntState.HuntFinished;
        TreasureHuntData.TreasureHuntInfo = null;
        TreasureHuntData.ChassesDones++;

        if (!TreasureHuntData.GiveUpRequested)
        {
            TreasureHuntData.ChassesSuccess++;
            TreasureHuntData.LastGiveUpReason = GiveUpReason.Success;
        }

        LogDiscord($"Chasse terminée: {TreasureHuntData.ChassesSuccess}/{TreasureHuntData.ChassesDones}");
        LogChassesFinished(TreasureHuntData.StartedAt,
                           TreasureHuntData.GiveUpRequested,
                           TreasureHuntData.LastGiveUpReason);
        TreasureHuntData.NextClue = null;

        Client.DoWork();
    }

    public void DoWorkTreasureHunt()
    {
        if (Map == null)
        {
            return;
        }

        if (GoOutIncarnam())
        {
            return;
        }

        var recurse = false;
        var mapId = Map.Id;

        LogInfo("TreasureHuntData.State: {State}", TreasureHuntData.State);

        switch (TreasureHuntData.State)
        {
            case TreasureHuntState.NoHuntActive:
            {
                if (mapId == 126878209)
                {
                    Map.GoToMap(126091543);
                    return;
                }

                if (mapId == 192416776)
                {
                    LogInfo("On est a la sortie d'incarnam, on bouge un peu");
                    Map.GoToMap(191106048);
                }

                if (mapId == 142087694)
                {
                    LogInfo("On va à l'intérieur pour récupérer une chasse");
                    Map.GoToMap(128452097);
                }
                else if (mapId == 128452097)
                {
                    LogInfo("On se positionne devant l'interactive pour récupérer une chasse");
                    Map.GoToCell(304);
                }
                else if (mapId == 128451073)
                {
                    if (FirstDoWork && Info.CellId == 292)
                    {
                        LogInfo("On est bug sur la case de mort, on va en arrière");
                        Map.GoToMap(142087694);
                    }
                    else
                    {
                        LogInfo("On va à l'intérieur pour récupérer une chasse");
                        Map.GoToMap(128452097);
                    }
                }
                else if (mapId == 142088718 || mapId == 128453121)
                {
                    LogInfo("On va à l'intérieur pour récupérer une chasse");
                    Map.GoToMap(128452097);
                }
                else if (AutoPath.Count == 0)
                {
                    if (!GoToSafeMapToTeleport() && !Map.IsHavenBag)
                    {
                        return;
                    }

                    if (Map.Data.CanUseHavenBag())
                    {
                        LogInfo("On peut rentrer dans un havre sac !");
                        HavenBag.EnterHavenBag(HavenBagEnterReason.TakeHunt);
                    }
                    else
                    {
                        LogInfo("On va à la salle des chasses à pied tant pis");
                        Map.GoToMap(Map.GetClosestZaapId());
                    }
                }

                break;
            }

            case TreasureHuntState.HuntActive:
            {
                if (mapId == 126878209)
                {
                    LogInfo("On est sur une map interdite, on abandonne la chasse");
                    TreasureHuntData.GiveUp(GiveUpReason.MapDisallowed);
                    Map.GoToMap(126091543);
                    return;
                }

                if (mapId == 147851781)
                {
                    LogInfo("On est sur une map interdite, on abandonne la chasse");
                    TreasureHuntData.GiveUp(GiveUpReason.MapDisallowed);
                }

                if (mapId == 128452097 && Map.CellId == 304)
                {
                    LogInfo("On est sur la map de la salle des chasses, on va donc sortir");
                    Map.GoToMap(142088718);
                }
                else if (mapId == 142088718 && Map.CellId == 356)
                {
                    LogInfo("On est à l'entrée des chasses, on va donc commencer la recherche en passant à l'havre sac");
                    HavenBag.EnterHavenBag(HavenBagEnterReason.GoToFirstStep);
                }
                else if (NeedToTakeHavenBagAsSoonAsPossible)
                {
                    LogInfo("On est au début d'une chasse, on prend donc un havre sac");
                    HavenBag.EnterHavenBag(HavenBagEnterReason.GoToFirstStep);
                }
                else
                {
                    if (TreasureHuntData.NextClue == null)
                    {
                        return;
                    }

                    if (AutoPathEndMapId == TreasureHuntData.NextClue.ToMapId &&
                        TreasureHuntData.NextClue.ToMapId != -1)
                    {
                        return;
                    }

                    if (TreasureHuntData.NextClue.ToMapId == -1)
                    {
                        TreasureHuntData.SolveNextClue();
                    }

                    var distanceFromNextClue = (int)Math.Sqrt(
                        Math.Pow(TreasureHuntData.NextClue.MapX - Map.Data.PosX, 2) +
                        Math.Pow(TreasureHuntData.NextClue.MapY - Map.Data.PosY, 2));

                    LogInfo("On est a {Distance} cases de la prochaine étape", distanceFromNextClue);

                    if (distanceFromNextClue > 60)
                    {
                        TreasureHuntData.SolveNextClue();
                        LogInfo("On prend donc un havre sac");
                        HavenBag.EnterHavenBag(HavenBagEnterReason.GoToFirstStep);
                        return;
                    }

                    Map.GoToMap(TreasureHuntData.NextClue.ToMapId);
                }

                break;
            }

            case TreasureHuntState.HuntFinished:
            {
                AutoPath = [];

                if (mapId == 128452097 && Map.CellId == 304)
                {
                    TreasureHuntData.State = TreasureHuntState.NoHuntActive;
                    TreasureHuntData.OnCellChanged();
                    return;
                }

                if (mapId == 142088718 && Map.CellId == 356)
                {
                    TreasureHuntData.State = TreasureHuntState.NoHuntActive;
                    Map.GoToMap(128452097);
                }
                else if (GoToSafeMapToTeleport() && !Map.IsHavenBag)
                {
                    TreasureHuntData.State = TreasureHuntState.NoHuntActive;
                    recurse = true;
                }

                break;
            }
        }

        if (recurse)
        {
            Client.DoWork();
        }
    }

    private bool GoToSafeMapToTeleport()
    {
        if (Map!.Data.CanUseHavenBag())
        {
            return true;
        }

        var mapHistories = TreasureHuntData.MapHistory.ToList();
        mapHistories.Reverse();

        foreach (var mapHistory in mapHistories)
        {
            if (!MapData.CanUseHavenBag(mapHistory))
            {
                continue;
            }

            Map.GoToMap(mapHistory.Id);
            return false;
        }

        Map.GoToMap(Map.GetClosestZaapId());
        return false;
    }

    private bool GoOutIncarnam()
    {
        if (Map == null)
        {
            return false;
        }

        var subArea = MapRepository.Instance.GetSubArea(Map.Data.SubAreaId);
        if (subArea == null || subArea.AreaId != 45)
        {
            return false;
        }

        if (Map.Id != 153880835)
        {
            LogInfo("On est à incarnam, on se casse");
            Map.GoToMap(153880835);
        }
        else
        {
            SendRequest(new NpcGenericActionRequest
                        {
                            NpcId = -20001,
                            NpcActionId = 3,
                            NpcMapId = Map.Id
                        },
                        NpcGenericActionRequest.TypeUrl);

            _ = SendRequestWithDelay(new NpcDialogReplyRequest
                                     {
                                         ReplyId = 36979,
                                         ReplyId2 = 36979
                                     },
                                     NpcDialogReplyRequest.TypeUrl,
                                     1000);

            _ = SendRequestWithDelay(new NpcDialogReplyRequest
                                     {
                                         ReplyId = 36977,
                                         ReplyId2 = 36977
                                     },
                                     NpcDialogReplyRequest.TypeUrl,
                                     2000);
        }

        return true;
    }
}
