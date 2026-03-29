using System.Diagnostics;
using Bubble.Core.Datacenter.Datacenter.World;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Maps;
using BubbleBot.Cli.Services.Maps.World;
using BubbleBot.Cli.Services.TreasureHunts.Models;
using Serilog;

namespace BubbleBot.Cli.Services.TreasureHunts;

public class FighterSimpleInfo
{
    public required long ActorId { get; set; }
    public required int CellId { get; set; }
    public required int MonsterId { get; set; }
}

public class TreasureHuntData
{
    public BotGameClient Client { get; }
    public int FighterToHit { get; set; }

    public TreasureHuntState State { get; set; } = TreasureHuntState.NoHuntActive;
    public TreasureHuntEvent? TreasureHuntInfo { get; set; }
    public Dictionary<long, FighterSimpleInfo> Fighters = new();

    public List<MapPositions> MapHistory { get; set; } = new();
    public long LastHintMapId { get; set; }

    public ClueStep? NextClue { get; set; }
    public bool GiveUpRequested { get; set; }
    public GiveUpReason? LastGiveUpReason { get; set; }

    public int SpellToUse { get; set; }
    public int SpellToUse2 { get; set; } = -1;

    public int ChassesDones { get; set; }
    public int ChassesSuccess { get; set; }

    private int AntiAfkCounter { get; set; }

    private int AntiAfkTriggerWithoutFlag { get; set; }
    private long _mapChangeUid;

    private CancellationTokenSource _antiAfkCts = new();

    public long StartedAt { get; private set; }

    public TreasureHuntData(BotGameClient client)
    {
        Client = client;
    }


    public async Task AntiAfk(long mapChangeUid, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        if (Client.Map == null)
            return;

        if (!Client.Connected)
        {
            return;
        }
        
        if(Client.IsBank || Client.IsAtDailyLimit || Client.IsKoli || Client.Trajet != null)
        {
            return;
        }

        var oldMap = Client.Map;

        // If we stay at the same map / cell for 1 minute, we try to do something
        await Task.Delay(20000, token);

        if (token.IsCancellationRequested)
            return;

        if (Client.IsInFight)
            return;

        if(Client.IsBank || Client.IsAtDailyLimit || Client.IsKoli)
        {
            return;
        }
        
        if (oldMap == Client.Map && _mapChangeUid == mapChangeUid)
        {
            AntiAfkCounter++;
            AntiAfkTriggerWithoutFlag++;

            if (AntiAfkTriggerWithoutFlag > 3)
            {
                Client.LogDiscord("Ça fait 3 fois qu'on est afk sans trouver d'indice, on abandonne", true);
                GiveUp(GiveUpReason.Afk);

                AntiAfkTriggerWithoutFlag = 0;

                await AntiAfk(mapChangeUid, token);
                return;
            }


            if (AntiAfkCounter >= 2 && Client.Map.Id == 88082193)
            {
                Client.Map.GoToMap(88081681);
                GiveUp(GiveUpReason.MapDisallowed);
                await AntiAfk(mapChangeUid, token);
            }

            if (Client.Map.Id == 188484104)
            {
                GiveUp(GiveUpReason.MapDisallowed);
                await AntiAfk(mapChangeUid, token);
                return;
            }
            
            if (Client.Map.Id == 203685888)
            {
                GiveUp(GiveUpReason.MapDisallowed);
                await AntiAfk(mapChangeUid, token);
                return;
            }

            if (Client.Map.Id == 139784)
            {
                Client.Map.GoToMap(73531910);
                GiveUp(GiveUpReason.MapDisallowed);
                await AntiAfk(mapChangeUid, token);
                return;
            }

            if (Client.Map.Id == 121766912)
            {
                GiveUp(GiveUpReason.MapDisallowed);
                await AntiAfk(mapChangeUid, token);
                return;
            }

            if (Client.Map.Data.SubAreaId == 469)
            {
                GiveUp(GiveUpReason.MapDisallowed);
                await AntiAfk(mapChangeUid, token);
                return;
            }

            if (AntiAfkCounter == 1)
            {
                Client.LogDiscord("On ne bouge plus, on retente de bouger", true);
                Client.DoWork();

                await AntiAfk(mapChangeUid, token);
            }
            else if (AntiAfkCounter == 2)
            {
                Client.LogDiscord("On ne bouge plus, on test de changer de map aléatoirement", true);

                var availableDirections = new List<Direction>();
                if (Client.Map.Data.RightMapId != 0)
                    availableDirections.Add(Direction.East);
                else if (Client.Map.Data.LeftMapId != 0)
                    availableDirections.Add(Direction.West);
                else if (Client.Map.Data.TopMapId != 0)
                    availableDirections.Add(Direction.North);
                else if (Client.Map.Data.BottomMapId != 0)
                    availableDirections.Add(Direction.South);

                if (availableDirections.Count > 0)
                {
                    var random = new Random();
                    var direction = availableDirections[random.Next(availableDirections.Count)];
                    Client.LogDiscord($"On change de map dans la direction {direction}", true);

                    switch (direction)
                    {
                        case Direction.East:
                            Client.Map.ToRight();
                            break;
                        case Direction.West:
                            Client.Map.ToLeft();
                            break;
                        case Direction.North:
                            Client.Map.ToTop();
                            break;
                        case Direction.South:
                            Client.Map.ToBottom();
                            break;
                    }
                }

                await AntiAfk(mapChangeUid, token);
            }
            else if (AntiAfkCounter >= 3)
            {
                GiveUp(GiveUpReason.Afk);

                if (!Client.Map.Data.CanUseHavenBag())
                {
                    if (Client.Map.GetClosestZaapId() != 0)
                    {
                        Client.Map.GoToMap(Client.Map.GetClosestZaapId());
                    }
                    else
                    {
                        if (Client.Connected)
                        {
                            Client.ReconnectFromScratch();
                        }
                        else
                        {
                            Client.LogDiscord("On n'est pas connecté", true);
                        }
                    }

                    await AntiAfk(mapChangeUid, token);
                    return;
                }

                Client.LogDiscord("On est afk depuis 3 minutes, on tente de se reconnecter", true);

                if (Client.Connected)
                {
                    Client.LogDiscord("On est déjà connecté, on se reconnecte", true);
                    Client.ReconnectFromScratch();
                }
                else
                {
                    Client.LogDiscord("On n'est pas connecté", true);
                }

                await AntiAfk(mapChangeUid, token);
            }
            else
            {
                Client.LogDiscord($"On est afk depuis {AntiAfkCounter} minutes, on abandonne");

                GiveUp(GiveUpReason.Afk);

                await AntiAfk(mapChangeUid, token);
            }
        }
        else
        {
            AntiAfkCounter = 0;
        }
    }

    public void OnMapChanged()
    {
        if (Client.Map == null)
            return;

        AntiAfkCounter = 0;
        _antiAfkCts.Cancel();
        _antiAfkCts = new CancellationTokenSource();
        _mapChangeUid++;

        Task.Run(async () => { await AntiAfk(_mapChangeUid, _antiAfkCts.Token); }, _antiAfkCts.Token);

        Fighters.Clear();
        FighterToHit = 0;

        MapHistory.Add(Client.Map.Data.Positions);

        if (NextClue != null && TreasureHuntInfo != null)
        {
            if (Client.Map.PhorreursOnCurrentMap.Contains(NextClue.PhorreurId))
            {
                Client.SendRequest(new TreasureHuntFlagRequest
                                   {
                                       QuestType = TreasureHuntInfo.QuestType,
                                       Index = TreasureHuntInfo.Flags.Count,
                                       QuestType2 = TreasureHuntInfo.QuestType
                                   },
                                   TreasureHuntFlagRequest.TypeUrl);
                AntiAfkTriggerWithoutFlag = 0;
                Client.LastFlagRequest = DateTime.UtcNow;
                return;
            }

            if (Client.Map.Id == 203685888)
            {
                // on doit attendre 60s après _lastHuntTakenAt
                GiveUp(GiveUpReason.MapDisallowed);
                return;
            }

            if (Client.Map.Id == 121766912)
            {
                // we give up
                // on doit attendre 60s après _lastHuntTakenAt

                GiveUp(GiveUpReason.MapDisallowed);
                return;
            }

            if (Client.Map.Id == NextClue.ToMapId)
            {
                if (NextClue.PhorreurId == 0)
                {
                    Client.SendRequest(new TreasureHuntFlagRequest
                                       {
                                           QuestType = TreasureHuntInfo.QuestType,
                                           Index = TreasureHuntInfo.Flags.Count,
                                           QuestType2 = TreasureHuntInfo.QuestType
                                       },
                                       TreasureHuntFlagRequest.TypeUrl);
                    Client.LastFlagRequest = DateTime.UtcNow;
                    AntiAfkTriggerWithoutFlag = 0;
                }

                if (NextClue.PhorreurId > 0)
                {
                    NextClue.FropMapId = Client.Map.Id;
                    NextClue.FromX = Client.Map.Data.PosX;
                    NextClue.FromY = Client.Map.Data.PosY;

                    SolveNextClue();
                }
            }
        }
    }

    public void OnCellChanged()
    {
        if (Client.Map == null)
            return;

        var mapId = Client.Map.Id;

        if (mapId == 128452097 && Client.Map.CellId == 304) // Salle des chasses
        {
            Client.LogInfo("On essaye de lancer une chasse");

            var interactive = Client.Map.GetInteractiveElements().FirstOrDefault(x => x.ElementTypeId == 231);

            if (interactive == null)
                return;

            Client.SendRequest(new InteractiveUseRequest
                               {
                                   ElementId = interactive.ElementId,
                                   SkillInstanceUid = interactive.EnabledSkills.First().SkillInstanceUid
                               },
                               InteractiveUseRequest.TypeUrl);
        }
    }

    public void OnDataReceived(TreasureHuntEvent treasureHuntEvent)
    {
        if (State != TreasureHuntState.HuntActive)
        {
            // On est sur une nouvelle chasse !
            StartedAt = Stopwatch.GetTimestamp();
            Client.NeedToTakeHavenBagAsSoonAsPossible = true;
        }

        State = TreasureHuntState.HuntActive;
        TreasureHuntInfo = treasureHuntEvent;
        LastHintMapId = treasureHuntEvent.StartMapId;
        GiveUpRequested = false;

        CurrentCheckpoint = treasureHuntEvent.CurrentCheckPoint + 1;
        TotalCheckpoint = treasureHuntEvent.TotalCheckPoint;
        
        if (TreasureHuntInfo.Flags.Count == TreasureHuntInfo.TotalStepCount)
        {
            _ = Client.SendRequestWithDelay(new TreasureHuntDigRequest
                               {
                                   QuestType = treasureHuntEvent.QuestType
                               },
                               TreasureHuntDigRequest.TypeUrl, 2000);
            return;
        }

        var lastFlag = TreasureHuntInfo.Flags.LastOrDefault();

        if (lastFlag != null)
        {
            LastHintMapId = lastFlag.MapId;
        }

        var lastStep = treasureHuntEvent.KnownSteps.LastOrDefault();

        if (lastStep != null)
        {
            var latestMap = MapRepository.Instance.GetMap(LastHintMapId);
            if (latestMap != null)
            {
                NextClue = new ClueStep
                {
                    FromX = latestMap.PosX,
                    FromY = latestMap.PosY,
                    FropMapId = LastHintMapId
                };

                switch (lastStep.StepCase)
                {
                    case TreasureHuntEvent.TreasureHuntStep.StepOneofCase.FollowDirectionToPoi:
                        NextClue.ClueId = lastStep.FollowDirectionToPoi.PoiLabelId;
                        NextClue.Direction = (Direction)lastStep.FollowDirectionToPoi.Direction;
                        break;
                    case TreasureHuntEvent.TreasureHuntStep.StepOneofCase.FollowDirectionToHint:
                        NextClue.PhorreurId = lastStep.FollowDirectionToHint.NpcId;
                        NextClue.Direction = (Direction)lastStep.FollowDirectionToHint.Direction;
                        break;
                    case TreasureHuntEvent.TreasureHuntStep.StepOneofCase.FollowDirection:
                        NextClue.PhorreurId = lastStep.FollowDirection.MapCount;
                        NextClue.Direction = (Direction)lastStep.FollowDirection.Direction;
                        break;

                    default:
                        break;
                }
            }
        }

        SolveNextClue();

        Client.DoWork();
    }

    public int CurrentCheckpoint { get; set; }

    public int TotalCheckpoint { get; set; }

    public void OnFightPlacementPossiblePositionsEvent(
        FightPlacementPossiblePositionsEvent fightPlacementPossiblePositionsEvent)
    {
        if (Client.Map == null)
            return;

        Fighters.Clear();

        // la meilleure cell c'est celle qui aura le moins de cellule non accessible autour d'elle
        var cellScores = new Dictionary<int, int>();

        foreach (var cellId in fightPlacementPossiblePositionsEvent.StartingPositions.ChallengersPositions)
        {
            var cell = Client.Map.Data.GetCell((short)cellId);

            if (cell == null)
                continue;

            // On check toute les cellules autour
            var score = 0;

            foreach (var direction in new[]
                         { Direction.NorthEast, Direction.SouthEast, Direction.SouthWest, Direction.NorthWest })
            {
                var nextCell =
                    Client.Map.Data.GetCell((short)MapTools.GetNextCellByDirection(cell.Id, (int)direction));

                if (nextCell == null)
                    continue;

                if (nextCell.NonWalkableDuringFight)
                    score += 10;

                if (!nextCell.Los)
                    score += 10;

                if (!nextCell.Mov)
                    score += 10;

                cellScores[cell.Id] = score;
            }
        }

        var bestCell = cellScores.OrderBy(x => x.Value).First().Key;

        _ = Client.SendRequestWithDelay(new FightPlacementPositionRequest
                                        {
                                            CellId = bestCell,
                                            EntityId = Client.PlayerId
                                        },
                                        FightPlacementPositionRequest.TypeUrl,
                                        550);

        _ = Client.SendRequestWithDelay(new FightReadyRequest
                                        {
                                            IsReady = true
                                        },
                                        FightReadyRequest.TypeUrl,
                                        3000);
    }

    public void OnFightEndEvent(FightEndEvent fightEndEvent) { }

    public void GiveUp(GiveUpReason reason)
    {
        if (State != TreasureHuntState.HuntActive)
            return;

        GiveUpRequested = true;
        LastGiveUpReason = reason;

        Client.SendRequest(new TreasureHuntGiveUpRequest
                           {
                               QuestType = TreasureHuntInfo?.QuestType ?? TreasureHuntType.Classic
                           },
                           TreasureHuntGiveUpRequest.TypeUrl);

        Client.LogDiscord("On abandonne la chasse");
    }

    public void SolveNextClue()
    {
        if (NextClue == null)
            return;

        if (NextClue.PhorreurId != 0)
        {
            var targetMapId = NextClue.FropMapId;
            var fromMap = MapRepository.Instance.GetMap(NextClue.FropMapId);
            var currentZoneId = Client.Map?.Data.GetCell((short)Client.Map.CellId);

            var found = false;

            if (currentZoneId != null)
            {
                var vertex = WorldPathFinderService.Instance.GetWorldGraph()
                                                   .GetVertex(targetMapId, currentZoneId.LinkedZoneRp);
                if (vertex != null)
                {
                    var outGoing = WorldPathFinderService.Instance.GetWorldGraph().GetOutgoingEdgesFromVertex(vertex);

                    var transitionEdge =
                        outGoing.FirstOrDefault(x => x.Transitions.Any(y => y.Direction == (int)NextClue.Direction));
                    if (transitionEdge != null)
                    {
                        targetMapId = transitionEdge.To.MapId;
                        found = true;
                    }
                }

                if (!found && fromMap != null)
                {
                    switch (NextClue.Direction)
                    {
                        case Direction.North:
                            targetMapId = MapRepository.Instance.GetMap(fromMap.TopMapIdServer) != null
                                ? fromMap.TopMapIdServer
                                : fromMap.TopMapId;
                            break;
                        case Direction.South:
                            targetMapId = MapRepository.Instance.GetMap(fromMap.BottomMapIdServer) != null
                                ? fromMap.BottomMapIdServer
                                : fromMap.BottomMapId;
                            break;
                        case Direction.East:
                            targetMapId = MapRepository.Instance.GetMap(fromMap.RightMapIdServer) != null
                                ? fromMap.RightMapIdServer
                                : fromMap.RightMapId;
                            break;
                        case Direction.West:
                            targetMapId = MapRepository.Instance.GetMap(fromMap.LeftMapIdServer) != null
                                ? fromMap.LeftMapIdServer
                                : fromMap.LeftMapId;
                            break;
                    }
                }
            }

            var targetMap = MapRepository.Instance.GetMap(targetMapId);

            if (targetMap == null && fromMap != null)
            {
                var targetX = fromMap.PosX;
                var targetY = fromMap.PosY;

                switch (NextClue.Direction)
                {
                    case Direction.North:
                        targetY -= 1;
                        break;
                    case Direction.South:
                        targetY += 1;
                        break;
                    case Direction.East:
                        targetX += 1;
                        break;
                    case Direction.West:
                        targetX -= 1;
                        break;
                }

                var subArea2 = MapRepository.Instance.GetSubArea(fromMap.SubAreaId);
                if (subArea2 != null)
                    targetMap = MapRepository.Instance.GetMap(targetX, targetY, fromMap.SubAreaId, subArea2.AreaId);
            }

            if (targetMap == null)
            {
                Client.LogInfo("Il y'à vraiment un soucis avec les phorreurs...");
            }

            NextClue.MapX = targetMap?.PosX ?? MapData.DecodeIdToX(targetMapId);
            NextClue.MapY = targetMap?.PosY ?? MapData.DecodeIdToY(targetMapId);
            NextClue.ToMapId = targetMapId;

            var lastHintMap = MapRepository.Instance.GetMap(LastHintMapId);

            if (lastHintMap != null)
            {
                var distanceFromOriginal = (int)Math.Sqrt(
                    Math.Pow(NextClue.MapX - lastHintMap.PosX, 2) +
                    Math.Pow(NextClue.MapY - lastHintMap.PosY, 2));

                if (distanceFromOriginal > 10)
                {
                    // we giveup
                    Client.LogInfo("On abandonne, on est trop loin de la map d'origine");
                    GiveUp(GiveUpReason.PhorreurTooFar);
                    return;
                }
            }

            if (NextClue.ToMapId <= 0)
            {
                Client.LogInfo($"Impossible de trouver la map {NextClue.MapX}, {NextClue.MapY}");

                GiveUp(GiveUpReason.MapNotFound);
                return;
            }

            return;
        }
        else
        {
            var (x, y) =
                CluesSolver.Instance.SolveClue(NextClue.ClueId,
                                               NextClue.FromX,
                                               NextClue.FromY,
                                               (int)NextClue.Direction);
            var (otherX, otherY) =
                CluesSolver.Instance.SolveClueFromLocal(NextClue.ClueId,
                                                        NextClue.FromX,
                                                        NextClue.FromY,
                                                        NextClue.Direction);

            if (x != otherX || y != otherY)
            {
                Client.LogWarning("SolveClueFromLocal and SolveClue are not the same");

                var distanceOne = (int)Math.Sqrt(Math.Pow(x - NextClue.FromX, 2) + Math.Pow(y - NextClue.FromY, 2));
                var distanceTwo =
                    (int)Math.Sqrt(Math.Pow(otherX - NextClue.FromX, 2) + Math.Pow(otherY - NextClue.FromY, 2));

                // we take the closer one
                if (distanceOne > distanceTwo)
                {
                    x = otherX;
                    y = otherY;
                }
            }

            if (x == 666 && y == 666)
            {
                Client.LogError("Failed to solve clue {ClueId}", NextClue.ClueId);
                Client.LogInfo($"Impossible de résoudre l'indice {NextClue.ClueId}");

                GiveUp(GiveUpReason.HintNotFound);
                return;
            }

            NextClue.MapX = x;
            NextClue.MapY = y;

            NextClue.Distance = (int)Math.Sqrt(Math.Pow(NextClue.FromX - NextClue.MapX, 2) +
                                               Math.Pow(NextClue.FromY - NextClue.MapY, 2));
            Client.LogInfo(
                $"Indice {NextClue.ClueId} résolu, direction {NextClue.Direction}, distance {NextClue.Distance}, x {NextClue.MapX}, y {NextClue.MapY}");
        }

        var startMap = MapRepository.Instance.GetMap(NextClue.FropMapId);

        if (startMap == null)
        {
            Client.LogInfo($"Impossible de trouver la map {NextClue.FropMapId}");

            GiveUp(GiveUpReason.HintNotFound);
            return;
        }

        var subArea = MapRepository.Instance.GetSubArea(startMap.SubAreaId);

        var toMap = MapRepository.Instance.GetMap(NextClue.MapX, NextClue.MapY, subArea!.Id, subArea.AreaId);
        NextClue.ToMapId = toMap?.Id ?? -1;

        if (NextClue.ToMapId == -1)
        {
            Client.LogInfo($"Impossible de trouver la map {NextClue.MapX}, {NextClue.MapY}");

            GiveUp(GiveUpReason.HintNotFound);
            return;
        }
    }

    public void OnFightAction(GameActionFightEvent actionFight)
    {
        //Client.LogInfo("Action {ActionId} from {SourceId}",
        //                actionFight.ActionId,
        //                actionFight.SourceId);
        
        if (actionFight.SummonsValue is { SummonsByContextInformationValue: not null })
        {
            foreach (var summon in actionFight.SummonsValue.SummonsByContextInformationValue.Summons)
            {
                var monsterId = summon.SpawnInformation.MonsterValue?.MonsterGid;

                foreach (var sum in summon.Summons)
                {
                    Fighters[sum.Position.ActorId] = new FighterSimpleInfo
                    {
                        ActorId = sum.Position.ActorId,
                        CellId = sum.Position.Disposition.CellId,
                        MonsterId = monsterId ?? 0
                    };
                }
            }
        }

       /* if (actionFight.SlideValue != null &&
            Fighters.TryGetValue(actionFight.SlideValue.TargetId, out var fighterSlide))
        {
            fighterSlide.CellId = actionFight.SlideValue.EndCell;
        }

        if (actionFight.ExchangePositionsValue != null &&
            Fighters.TryGetValue(actionFight.ExchangePositionsValue.TargetId, out var fighterExchange))
        {
            fighterExchange.CellId = actionFight.ExchangePositionsValue.TargetCellId;
        }
*/
        if (actionFight.ChangeLookValue != null && actionFight.ChangeLookValue.TargetId < 0)
        {
            // Client.LogInfo("On va taper sur {FighterToHit}", actionFight.ChangeLookValue.TargetId);
            FighterToHit = (int)actionFight.ChangeLookValue.TargetId;
        }

        _ = Client.SendRequestWithDelay(new GameActionAcknowledgementRequest
                                        {
                                            Valid = true,
                                            ActionId = actionFight.ActionId,
                                            ActionId2 = actionFight.ActionId
                                        },
                                        GameActionAcknowledgementRequest.TypeUrl,
                                        Random.Shared.Next(1000, 3000));
    }

    public void OnFightTurnEvent(FightTurnEvent fightTurnEvent)
    {
        if (fightTurnEvent.CharacterId != Client.PlayerId)
        {
            return;
        }

        if (FighterToHit == 0)
        {
            return;
        }

        SetSpellToUse(SpellToUse);
        SetSpell2ToUse(SpellToUse2);

        Task.Run(async () =>
        {
            Client.SendRequest(new GameActionFightCastOnTargetRequest
                               {
                                   SpellId = SpellToUse,
                                   TargetId = FighterToHit,
                                   Eekds = null,
                               },
                               GameActionFightCastOnTargetRequest.TypeUrl);

            await Task.Delay(Random.Shared.Next(1000, 1500));

            Client.SendRequest(new GameActionFightCastOnTargetRequest
                               {
                                   SpellId = SpellToUse,
                                   TargetId = FighterToHit,
                                   Eekds = null,
                               },
                               GameActionFightCastOnTargetRequest.TypeUrl);
            await Task.Delay(Random.Shared.Next(1000, 1500));

            Client.SendRequest(new GameActionFightCastOnTargetRequest
                               {
                                   SpellId = SpellToUse2 >= 0
                                       ? SpellToUse2
                                       : SpellToUse,
                                   TargetId = FighterToHit,
                                   Eekds = null,
                               },
                               GameActionFightCastOnTargetRequest.TypeUrl);

            Client.SendRequest(new FightTurnFinishRequest
                               {
                                   IsAfk = false,
                                   IsAfk2 = false
                               },
                               FightTurnFinishRequest.TypeUrl);
        });
    }


    public void SetSpellToUse(int shortcutSpellSpellId)
    {
        SpellToUse = shortcutSpellSpellId;

        try
        {
            if (Client.Info.Information?.CharacterLook?.BreedId == 3)
            {
                // Enu
                SpellToUse = 13331;
            }
            else if (Client.Info.Information?.CharacterLook?.BreedId == 11)
            {
                // Sacri
                SpellToUse = 12728;
            }
            else if (Client.Info.Information?.CharacterLook?.BreedId == 5)
            {
                // Xel Gelure
                SpellToUse = 13245;
            }
            else if (Client.Info.Information?.CharacterLook?.BreedId == 16)
            {
                // Eliotrope
                SpellToUse = 14593;
            }
            else if (Client.Info.Information?.CharacterLook?.BreedId == 10)
            {
                // Sadida
                SpellToUse = 13528;
            }
        }
        catch (Exception e)
        {
            Client.LogError(e, "Error while setting spell to use");
        }
    }

    public void SetSpell2ToUse(int shortcutSpellSpellId)
    {
        SpellToUse2 = shortcutSpellSpellId;

        try
        {
            if (Client.Info.Information?.CharacterLook.BreedId == 3)
            {
                // Enu
                SpellToUse2 = 13331;
            }
            else if (Client.Info.Information?.CharacterLook.BreedId == 11)
            {
                // Sacri
                SpellToUse2 = 12728;
            }
            else if (Client.Info.Information?.CharacterLook?.BreedId == 5)
            {
                // Xel Souvenir
                SpellToUse2 = 13281;
            }
            else if (Client.Info.Information?.CharacterLook?.BreedId == 16)
            {
                // Eliotrope
                SpellToUse2 = 14593;
            }   
            else if (Client.Info.Information?.CharacterLook?.BreedId == 10)
            {
                // Sadida
                SpellToUse2 = 13574;
            }
        }
        catch (Exception e)
        {
            Client.LogError(e, "Error while setting spell to use");
        }
    }

    public void OnFlagAnswer(TreasureHuntFlagAnswerEvent treasureHuntFlagAnswerEvent)
    {
        if (treasureHuntFlagAnswerEvent.Result != TreasureHuntFlagAnswerEvent.FlagResult.Ok)
        {
            Client.LogDiscord($"Impossible de poser le drapeau, on abandonne ({treasureHuntFlagAnswerEvent.Result})");
            GiveUp(GiveUpReason.HintNotFound);
        }
    }

    public void OnDigAnswer(TreasureHuntDigAnswerEvent treasureHuntDigAnswerEvent)
    {
        if (treasureHuntDigAnswerEvent.Result == TreasureHuntDigAnswerEvent.DigResult.Wrong ||
            treasureHuntDigAnswerEvent.Result == TreasureHuntDigAnswerEvent.DigResult.WrongAndYouKnowIt)
        {
            Client.LogDiscord($"Impossible de valider l'étape, on abandonne ({treasureHuntDigAnswerEvent.Result})");
            GiveUp(GiveUpReason.HintNotFound);
        }
    }
}